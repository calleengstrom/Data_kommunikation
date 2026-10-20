
using ESP32Config;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;


namespace BLE_app
{
    public sealed partial class MainWindow : Window
    {
        private readonly BleService _bleService = new();

        public MainWindow()
        {
            InitializeComponent();

            _bleService.DeviceFound += OnDeviceFound;

            ScanButton.Click += ScanButton_Click;
            ConnectButton.Click += ConnectButton_Click;

            Closed += (_, _) => _bleService.Dispose();
        }

        private void OnDeviceFound(BleDeviceInfo device)
        {
            // BLE-events kommer från en annan tråd.
            // Uppdatera därför GUI:t via DispatcherQueue.
            DispatcherQueue.TryEnqueue(() =>
            {
                DeviceComboBox.Items.Add(device);
            });
        }

        private void ScanButton_Click(
            object sender, RoutedEventArgs e)
        {
            DeviceComboBox.Items.Clear();

            ConnectionStatusText.Text = "Scanning...";

            _bleService.StartScan();
        }

        private async void ConnectButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem is not BleDeviceInfo device)
                return;

            ConnectButton.IsEnabled = false;
            ConnectionStatusText.Text = "Connecting...";

            try
            {
                bool connected =
                    await _bleService.ConnectAsync(device.Address);

                if (connected)
                {
                    ConnectionStatusText.Text = "Connected";






                    ConnectionIndicator.Fill =
                        new SolidColorBrush(Color.FromArgb(
                            255, 34, 197, 94));

                    // Kontrollera GATT-characteristics
                    var characteristics =
                        await _bleService.GetCharacteristicsAsync();

                    System.Diagnostics.Debug.WriteLine(
                        $"Found {characteristics.Count} characteristics");

                    foreach (var characteristic in characteristics)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            characteristic.Uuid.ToString());
                    }
                    await LoadDeviceSettingsAsync();
                }
                else
                {
                    ShowDisconnected("Connection failed");
                }
            }
            catch (Exception ex)
            {
                ShowDisconnected("Connection failed");

                System.Diagnostics.Debug.WriteLine($"ERROR  :  { ex.Message}");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }


        private async Task LoadDeviceSettingsAsync()
        {
            string uuidPrefix = "08e4b47a-d691-41ab-b37a-28a37dbe";

            var fields = new Dictionary<int, TextBox>
    {
        { 2, DeviceIdTextBox },
        { 3, WifiSSIDTextBox },
        { 5, MqttHostTextBox },
        { 6, LocationTextBox },
        { 8, MqttUsernameTextBox },
        { 10, MqttTopicTextBox }
    };

            foreach (var field in fields)
            {
                string uuidString = uuidPrefix + field.Key.ToString("D4");
                Guid uuid = Guid.Parse(uuidString);
                System.Diagnostics.Debug.WriteLine($"Reading UUID: {uuid}");
                string value = await _bleService.ReadCharacteristicAsync(uuid);

                field.Value.Text = value;
                field.Value.IsEnabled = true;
            }

            // MQTT-port hanteras separat eftersom den använder NumberBox
            Guid portUuid = Guid.Parse(uuidPrefix + "0007");

            string portValue = await _bleService.ReadCharacteristicAsync(portUuid);

            if (double.TryParse(portValue, out double port))
            {
                MqttPortNumberBox.Value = port;
            }

            MqttPortNumberBox.IsEnabled = true;
        }



        private void ShowDisconnected(string message)
        {
            ConnectionStatusText.Text = message;

            ConnectionIndicator.Fill =
                new SolidColorBrush(Color.FromArgb(
                    255, 239, 68, 68));
        }
    }
}
