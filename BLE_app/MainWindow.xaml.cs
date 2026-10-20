
using ESP32Config;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;


namespace BLE_app
{
    public sealed partial class MainWindow : Window
    {
        private readonly BleService _bleService = new();

        private const string uuidPrefix = "08e4b47a-d691-41ab-b37a-28a37dbe";

        private readonly Dictionary<int, string> _savedValues = new();

        private bool _loading = false;
        private bool _connected = false;

        private Guid GetUuid(int num) =>
            Guid.Parse(uuidPrefix + num.ToString("D4"));


        private Dictionary<int, TextBox> GetTextFields() => new()
        {
            { 2, DeviceIdTextBox },
            { 3, WifiSSIDTextBox },
            { 5, MqttHostTextBox },
            { 6, LocationTextBox },
            { 8, MqttUsernameTextBox },
            { 10, MqttTopicTextBox }
        };


        public MainWindow()
        {
            InitializeComponent();


            _bleService.DeviceFound += OnDeviceFound;

            _bleService.Disconnected += BleService_Disconnected;

            ScanButton.Click += ScanButton_Click;
            ConnectButton.Click += ConnectButton_Click;


            SaveButton.Click += SavedButton_Click;

            CancelButton.Click += CancelButton_Click;

            foreach (var field in GetTextFields().Values)
                field.TextChanged += (_, _) => UpdateChanges();

            MqttPortNumberBox.ValueChanged +=
                (_, _) => UpdateChanges();

            WifiPasswordBox.PasswordChanged +=
                (_, _) => UpdateChanges();

            MqttPasswordBox.PasswordChanged +=
                (_, _) => UpdateChanges();

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
                _connected = false;

                bool connected =
                    await _bleService.ConnectAsync(device.Address);


                if (!connected)
                {
                    throw new Exception("Ble connection failed");
                }
                _connected = true;


                ConnectionStatusText.Text = "Connected";


                ConnectionIndicator.Fill =
                    new SolidColorBrush(Color.FromArgb(
                        255, 34, 197, 94));

                await LoadDeviceSettingsAsync();

            }
            catch (Exception ex)
            {
                _connected = false;

                ConnectionStatusText.Text = "Connection Error";
                SaveStatusText.Text = ex.Message;
                ShowDisconnected("Connection failed");

                System.Diagnostics.Debug.WriteLine($"ERROR  :  {ex.Message}");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }


        private async Task LoadDeviceSettingsAsync()
        {

            _loading = true;


            try
            {
                _savedValues.Clear();

                foreach (var field in GetTextFields())
                {
                    string value = await _bleService.ReadCharacteristicAsync(GetUuid(field.Key));

                    field.Value.Text = value;
                    field.Value.IsEnabled = true;

                    _savedValues[field.Key] = value;
                }

                string portValue = await _bleService
                    .ReadCharacteristicAsync(GetUuid(7));

                if (!double.TryParse(portValue, out double port))
                    throw new Exception("Invalid mqtt port");

                MqttPortNumberBox.Value = port;
                MqttPortNumberBox.IsEnabled = true;
                _savedValues[7] = portValue;

                WifiPasswordBox.Password = "";
                MqttPasswordBox.Password = "";

                WifiPasswordBox.IsEnabled = true;
                MqttPasswordBox.IsEnabled = true;

                CancelButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                SaveStatusText.Text = "No changes";
            }
            finally
            {
                _loading = false;
            }
        }

        private Dictionary<int, string> GetCurrentValues()
        {
            var values = new Dictionary<int, string>();

            foreach (var field in GetTextFields())
            {
                values[field.Key] = field.Value.Text;
            }

            values[7] = double.IsNaN(MqttPortNumberBox.Value) ? "" :
                MqttPortNumberBox.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

            return values;

        }

        private void UpdateChanges()
        {
            if (_loading || !_connected || _savedValues.Count == 0) { return; }

            var current = GetCurrentValues();

            bool changed = current.Any(field =>
                !_savedValues.TryGetValue(field.Key, out string? oldValue)
                || oldValue != field.Value);

            changed |= !string.IsNullOrEmpty(WifiPasswordBox.Password);
            changed |= !string.IsNullOrEmpty(MqttPasswordBox.Password);

            SaveButton.IsEnabled = changed;
            CancelButton.IsEnabled = changed;

            SaveStatusText.Text = changed
                ? "Unsaved changes"
                : "No changes";

        }

        private async void SavedButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SaveStatusText.Text = "Saving...";

            try
            {
                var current = GetCurrentValues();

                foreach (var field in current)
                {
                    if (!_savedValues.TryGetValue(field.Key, out string? oldValue) || field.Value != oldValue)
                    {
                        await _bleService.WriteCharacteristicAsync(GetUuid(field.Key), field.Value);
                    }
                }

                if (!string.IsNullOrEmpty(WifiPasswordBox.Password))
                {
                    await _bleService.WriteCharacteristicAsync(GetUuid(4), WifiPasswordBox.Password);
                }
                if (!string.IsNullOrEmpty(MqttPasswordBox.Password))
                {
                    await _bleService.WriteCharacteristicAsync(GetUuid(9), MqttPasswordBox.Password);
                }

                await _bleService.WriteCharacteristicAsync(GetUuid(11), "SAVE");

                string status = await _bleService.ReadCharacteristicAsync(GetUuid(12));

                if (status != "SAVE_REQUESTED")
                {
                    throw new Exception($"ESP32 {status}");
                }

                
                SaveStatusText.Text = "Settings saved. ESP32 restarting...";

                await _bleService.DisconnectAsync();

            }
            catch (Exception ex)
            {
                SaveStatusText.Text = $"Saved failed : {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                UpdateChanges();
            }

        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton.IsEnabled = false;
            SaveButton.IsEnabled = false;

            try
            {
                await _bleService.WriteCharacteristicAsync(GetUuid(11), "CANCEL");

                await LoadDeviceSettingsAsync();
                SaveStatusText.Text = "Changes cancelled";
            }
            catch(Exception ex)
            {
            
                SaveStatusText.Text = $"Cancelled failed :{ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);           
            }
            finally 
            {
                UpdateChanges(); 
            }
        }



        private void BleService_Disconnected()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _connected = false;

                ShowDisconnected("Disconnected");

                SaveButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
            });
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
