
using ESP32Config;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
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
                }
                else
                {
                    ShowDisconnected("Connection failed");
                }
            }
            catch (Exception ex)
            {
                ShowDisconnected("Connection failed");

                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
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
