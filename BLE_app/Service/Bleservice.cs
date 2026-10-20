
using System;
using System.Collections.Generic;
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
        private static readonly Guid ServiceUuid =
            Guid.Parse("08e4b47a-d691-41ab-b37a-28a37dbe0001");

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

            bool isOurDevice =
                name.StartsWith("ESP32-Config",
                    StringComparison.OrdinalIgnoreCase)
                || args.Advertisement.ServiceUuids.Contains(ServiceUuid);

            if (!isOurDevice)
                return;

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
                Name = string.IsNullOrWhiteSpace(name)
                    ? "ESP32-Config"
                    : name
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
            Disconnect();
            StopScan();

            _device = await BluetoothLEDevice
                .FromBluetoothAddressAsync(address);

            if (_device == null)
                return false;

            var result = await _device.GetGattServicesForUuidAsync(
                ServiceUuid,
                BluetoothCacheMode.Uncached
            );

            if (result.Status != GattCommunicationStatus.Success ||
                result.Services.Count == 0)
            {
                Disconnect();
                return false;
            }

            _gattService = result.Services[0];

            return true;
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
            _gattService?.Dispose();
            _gattService = null;

            _device?.Dispose();
            _device = null;
        }

        public void Dispose()
        {
            StopScan();
            Disconnect();
        }
    }
}
