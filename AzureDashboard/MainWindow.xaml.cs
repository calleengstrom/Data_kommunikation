
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;
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

        private readonly string? _connectionString =
            Environment.GetEnvironmentVariable(
                "AZURE_EVENTHUB_CONNECTION_STRING")?? "xxx";


        public MainWindow()
        {
            InitializeComponent();

            Title = "Azure Sensor Dashboard";

            AppWindow.Resize(
                new Windows.Graphics.SizeInt32(800, 500));

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
                    var json =
                        receivedEvent.Data.EventBody.ToString();

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
            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            var result = new SensorData
            {
                GatewayId = root.GetProperty("DeviceId")
                                .GetString() ?? "--",

                SentAt = root.GetProperty("SentAt")
                             .GetDateTimeOffset()
            };

            var devices = root.GetProperty("Measurements");

            if (devices.GetArrayLength() == 0)
                throw new JsonException("No devices found.");

            // Display the first device in this version.
            var device = devices[0];

            result.DeviceId =
                device.GetProperty("DeviceId").GetString() ?? "--";

            result.Location =
                device.GetProperty("Location").GetString() ?? "--";

            foreach (var measurement in
                device.GetProperty("Measurements").EnumerateArray())
            {
                var type = measurement
                    .GetProperty("MeasurementType")
                    .GetString();

                var value = measurement
                    .GetProperty("Value")
                    .GetDouble();

                var unit = measurement
                    .GetProperty("Unit")
                    .GetString() ?? "";

                if (string.Equals(type, "Temperature",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "Temperatue",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Temperature = value;
                    result.TemperatureUnit = unit;
                }
                else if (string.Equals(type, "Humidity",
                             StringComparison.OrdinalIgnoreCase))
                {
                    result.Humidity = value;
                    result.HumidityUnit = unit;
                }
            }

            return result;
        }

        private void UpdateDashboard(SensorData data)
        {
            DeviceIdText.Text = data.DeviceId;
            LocationText.Text = $"Location: {data.Location}";

            var culture = CultureInfo.GetCultureInfo("sv-SE");

            TemperatureText.Text = data.Temperature.HasValue
                ? $"{data.Temperature.Value.ToString("0.0", culture)} {data.TemperatureUnit}"
                : "--";

            HumidityText.Text = data.Humidity.HasValue
                ? $"{data.Humidity.Value.ToString("0.0", culture)} {data.HumidityUnit}"
                : "--";

            GatewayText.Text = $"Gateway: {data.GatewayId}";

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

    public sealed class SensorData
    {
        public string GatewayId { get; set; } = "--";
        public string DeviceId { get; set; } = "--";
        public string Location { get; set; } = "--";

        public double? Temperature { get; set; }
        public string TemperatureUnit { get; set; } = "°C";

        public double? Humidity { get; set; }
        public string HumidityUnit { get; set; } = "%";

        public DateTimeOffset SentAt { get; set; }
    }
}
