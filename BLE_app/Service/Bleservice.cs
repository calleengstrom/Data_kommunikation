
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace ESP32Config
{
    public class BleDeviceInfo
    {
        public ulong Address { get; set; }
        public string Name { get; set; } = "";

        public override string ToString()
        {
            return $"{Name} ({Address:X12})";
        }
    }

    public class BleService : IDisposable
    {
        public string? UuidPrefix { get; private set; }

        public Guid GetCharacteristicUuid(int number)
        {
            if (UuidPrefix == null)
                throw new InvalidOperationException("No BLE service discovered.");

            return Guid.Parse(UuidPrefix + number.ToString("D4"));
        }


        private BluetoothLEAdvertisementWatcher? _watcher;
        private BluetoothLEDevice? _device;
        private GattDeviceService? _gattService;

        private readonly HashSet<ulong> _foundAddresses = new();
        private readonly object _scanLock = new();

        public event Action<BleDeviceInfo>? DeviceFound;

        public bool IsConnected =>
            _device?.ConnectionStatus ==
            BluetoothConnectionStatus.Connected
            && _gattService != null;

        public void StartScan()
        {
            StopScan();

            lock (_scanLock)
            {
                _foundAddresses.Clear();
            }

            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            _watcher.Received += OnAdvertisementReceived;
            _watcher.Start();
        }

        private void OnAdvertisementReceived(
            BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string name = args.Advertisement.LocalName;

            if (string.IsNullOrEmpty(name) ||
                !name.StartsWith("ESP32",
            StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool isNew;

            lock (_scanLock)
            {
                isNew = _foundAddresses.Add(args.BluetoothAddress);
            }

            if (!isNew)
                return;

            DeviceFound?.Invoke(new BleDeviceInfo
            {
                Address = args.BluetoothAddress,
                Name = name
            });
        }

        public void StopScan()
        {
            if (_watcher == null)
                return;

            _watcher.Received -= OnAdvertisementReceived;

            if (_watcher.Status ==
                BluetoothLEAdvertisementWatcherStatus.Started)
            {
                _watcher.Stop();
            }

            _watcher = null;
        }


        public async Task<bool> ConnectAsync(ulong address)
        {
            CleanupConnection();
            StopScan();

            UuidPrefix = null;

            _device = await BluetoothLEDevice
                .FromBluetoothAddressAsync(address);

            if (_device == null)
                return false;

            var result = await _device.GetGattServicesAsync(
                BluetoothCacheMode.Uncached);

            if (result.Status != GattCommunicationStatus.Success)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GATT discovery failed: {result.Status}");

                Disconnect();
                return false;
            }

            foreach (var service in result.Services)
            {
                string serviceUuid = service.Uuid.ToString("D");

                // Konfigurationstjänsten ska sluta på 0001
                if (!serviceUuid.EndsWith("0001"))
                    continue;

                string prefix = serviceUuid[..^4];

                var chars = await service.GetCharacteristicsAsync(
                    BluetoothCacheMode.Uncached);

                if (chars.Status != GattCommunicationStatus.Success)
                    continue;

                var uuids = chars.Characteristics
                    .Select(c => c.Uuid)
                    .ToHashSet();

                // Kontrollera att tjänsten har våra characteristics
                int[] required = { 2, 3, 5, 6, 7, 8, 10, 11, 12 };

                bool isValid = required.All(number =>
                    uuids.Contains(Guid.Parse(
                        prefix + number.ToString("D4"))));

                if (!isValid)
                    continue;

                // Vi har hittat rätt tjänst!
                _gattService = service;
                UuidPrefix = prefix;

                _device.ConnectionStatusChanged +=
                    Device_ConnectionStatusChanged;

                System.Diagnostics.Debug.WriteLine(
                    $"Connected to ESP32 with UUID prefix: {UuidPrefix}");

                foreach (var other in result.Services)
                {
                    if (!ReferenceEquals(other, _gattService))
                        other.Dispose();
                }

                return true;
            }

            foreach (var service in result.Services)
                service.Dispose();

            Disconnect();
            return false;
        }


        public async Task<IReadOnlyList<GattCharacteristic>>
            GetCharacteristicsAsync()
        {
            if (_gattService == null)
                throw new InvalidOperationException(
                    "No ESP32 connected.");

            var result = await _gattService.GetCharacteristicsAsync(
                BluetoothCacheMode.Uncached
            );

            if (result.Status != GattCommunicationStatus.Success)
                throw new Exception(
                    "Could not retrieve BLE characteristics.");

            return result.Characteristics;
        }




        public void Disconnect()
        {
            bool wasConnected =
                _device != null || _gattService != null;

            CleanupConnection();

            if (wasConnected)
            {
                Disconnected?.Invoke();
            }
        }



        public Task DisconnectAsync()
        {
            Disconnect();
            return Task.CompletedTask;
        }


        private void CleanupConnection()
        {
            if (_device != null)
            {
                _device.ConnectionStatusChanged -= Device_ConnectionStatusChanged;
            }

            _gattService?.Dispose();
            _gattService = null;

            _device?.Dispose();
            _device = null;

            UuidPrefix = null;
        }


        public async Task<string> ReadCharacteristicAsync(Guid uuid)
        {
            if (_gattService == null)
                throw new InvalidOperationException("Not connected to ESP32.");

            // Hitta rätt characteristic med UUID
            var result = await _gattService.GetCharacteristicsForUuidAsync(
                uuid,
                BluetoothCacheMode.Uncached
            );

            if (result.Status != GattCommunicationStatus.Success ||
                result.Characteristics.Count == 0)
            {
                throw new Exception("Characteristic not found.");
            }

            // Läs värdet från ESP32
            var characteristic = result.Characteristics[0];

            var readResult = await characteristic.ReadValueAsync(
                BluetoothCacheMode.Uncached
            );

            if (readResult.Status != GattCommunicationStatus.Success)
                throw new Exception("Failed to read characteristic.");

            // Konvertera mottagna bytes till String
            var reader = Windows.Storage.Streams.DataReader.FromBuffer(
                readResult.Value
            );

            byte[] bytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(bytes);

            return System.Text.Encoding.UTF8.GetString(bytes);
        }


        public async Task WriteCharacteristicAsync(Guid uuid, string value)
        {
            if (_gattService == null)
                throw new InvalidOperationException("Not connected to ESP32.");

            var result = await _gattService.GetCharacteristicsForUuidAsync(
                uuid, BluetoothCacheMode.Uncached);

            if (result.Status != GattCommunicationStatus.Success ||
                result.Characteristics.Count == 0)
                throw new Exception($"Characteristic not found: {uuid}");

            var characteristic = result.Characteristics[0];

            var writer = new Windows.Storage.Streams.DataWriter();
            writer.WriteBytes(System.Text.Encoding.UTF8.GetBytes(value));

            var status = await characteristic.WriteValueAsync(
                writer.DetachBuffer(),
                GattWriteOption.WriteWithResponse);

            if (status != GattCommunicationStatus.Success)
                throw new Exception($"BLE write failed: {uuid}");
        }

        public event Action? Disconnected;

        private void Device_ConnectionStatusChanged(
        BluetoothLEDevice sender,
        object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                Disconnected?.Invoke();
            }
        }


        public void Dispose()
        {
            StopScan();
            Disconnect();
        }
    }


}
