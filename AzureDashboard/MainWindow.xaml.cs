
using Azure.Messaging.EventHubs.Consumer;
using AzureDashboard.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace AzureDashboard
{
    public sealed partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;
        private Task? _consumerTask;

        public ObservableCollection<DeviceViewModel> Devices { get; }
            = new();

        private readonly string? _connectionString =
            Environment.GetEnvironmentVariable(
                "AZURE_EVENTHUB_CONNECTION_STRING") ??
            "";
        public MainWindow()
        {
            InitializeComponent();

            Title = "Azure Sensor Dashboard";

            AppWindow.Resize(
                new Windows.Graphics.SizeInt32(900, 550));

            Closed += (_, _) => _cts?.Cancel();
        }

        private void ConnectButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_consumerTask is { IsCompleted: false })
                return;

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                SetStatus("Missing connection string", false);
                return;
            }

            _cts = new CancellationTokenSource();

            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;

            SetStatus("Connecting...", false);

            _consumerTask = ReadAzureEventsAsync(_cts.Token);
        }

        private void DisconnectButton_Click(
            object sender, RoutedEventArgs e)
        {
            DisconnectButton.IsEnabled = false;
            SetStatus("Disconnecting...", false);

            _cts?.Cancel();
        }

        private async Task ReadAzureEventsAsync(
            CancellationToken ct)
        {
            try
            {
                await using var consumer =
                    new EventHubConsumerClient(
                        EventHubConsumerClient.DefaultConsumerGroupName,
                        _connectionString!);

                DispatcherQueue.TryEnqueue(() =>
                {
                    SetStatus("Connected", true);
                });

                await foreach (var receivedEvent in
                    consumer.ReadEventsAsync(
                        startReadingAtEarliestEvent: false,
                        cancellationToken: ct))
                {
                    var json = receivedEvent.Data.EventBody.ToString();

                    try
                    {
                        var data = ParseMessage(json);

                        DispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateDashboard(data);
                        });
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed JSON messages.
                    }
                }
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                // Normal disconnect.
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    SetStatus($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ConnectButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;

                    if (ct.IsCancellationRequested)
                        SetStatus("Disconnected", false);
                });
            }
        }

        private static SensorData ParseMessage(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result =
                JsonSerializer.Deserialize<SensorData>(json, options);

            if (result == null)
                throw new JsonException("Failed to parse sensor data.");

            return result;
        }

        public void UpdateDashboard(SensorData data)
        {
            GatewayText.Text = $"Gateway: {data.GatewayId}";

            if (data.MeasurementsPackage != null)
            {
                foreach (var device in data.MeasurementsPackage)
                {
                    if (string.IsNullOrWhiteSpace(device.DeviceId))
                        continue;

                    // Find existing device using DeviceId
                    var viewModel = Devices.FirstOrDefault(
                        d => d.DeviceId == device.DeviceId);

                    // Add new device if it doesn't exist
                    if (viewModel == null)
                    {
                        viewModel = new DeviceViewModel(device.DeviceId);
                        Devices.Add(viewModel);
                    }

                    // Update device information
                    viewModel.Location =
                        $"Location: {device.Location}";

                    viewModel.ReceivedAt = device.ReceivedAt == default
                        ? "Received at: --"
                        : $"Received at: {device.ReceivedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

                    if (device.Measurements == null)
                        continue;

                    // Temperature
                    var temperature = device.Measurements
                        .FirstOrDefault(m =>
                            m.MeasurementType == "Temperature" ||
                            m.MeasurementType == "Temperatue");

                    if (temperature != null)
                    {
                        viewModel.Temperature =
                            $"{temperature.Value:0.0}{temperature.Unit}";
                    }

                    // Humidity
                    var humidity = device.Measurements
                        .FirstOrDefault(m =>
                            m.MeasurementType == "Humidity");

                    if (humidity != null)
                    {
                        viewModel.Humidity =
                            $"{humidity.Value:0.0}{humidity.Unit}";
                    }
                }
            }

            LastUpdateText.Text =
                $"Last update: {data.SentAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        }

        private void SetStatus(string message, bool connected)
        {
            ConnectionStatus.Text = message;

            ConnectionStatus.Foreground =
                new SolidColorBrush(
                    connected
                        ? Color.FromArgb(255, 74, 222, 128)
                        : Color.FromArgb(255, 248, 113, 113));
        }
    }
}
