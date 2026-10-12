
using Azure.Messaging.EventHubs.Consumer;
using AzureDashboard.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;
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

        private readonly string? _connectionString =
            Environment.GetEnvironmentVariable(
                "AZURE_EVENTHUB_CONNECTION_STRING")?? 
            "xxx";


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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<SensorData>(json, options);

            if (result == null)
                throw new JsonException("Failed to parse sensor data.");

            return result;
        }


        public void UpdateDashboard(SensorData data)
        {

            //reflektion, om man vill ha en mer dynamsik update kan man köra en foreach 

            //foreach(var dm in data.MeasurementsPackage)
            //{
            //    set varje rad till respektive text / data
            //}
            // Dock behöver man en dynamsik panel / gui som lägger till flera rader per device



            GatewayText.Text = $"Gateway: {data.GatewayId}";
            


            var deviceMeasurements = data.MeasurementsPackage.First();
            

            DeviceIdText.Text = deviceMeasurements.DeviceId;


            var temperature = deviceMeasurements.Measurements
                .FirstOrDefault(m => m.MeasurementType == "Temperatue");

            var humidity = deviceMeasurements.Measurements
                .FirstOrDefault(m => m.MeasurementType == "Humidity");
           
            if (temperature == null || humidity == null) 
            {
                return;

            }

            var culture = CultureInfo.GetCultureInfo("sv-SE");

            LocationText.Text = $"Location: {deviceMeasurements.Location}";

            TemperatureText.Text = temperature.Value.ToString();

                //? $"{data.Temperature.Value.ToString("0.0", culture)} {data.TemperatureUnit}"
                //: "--";

            HumidityText.Text = humidity.Value.ToString();

                //? $"{data.Humidity.Value.ToString("0.0", culture)} {data.HumidityUnit}"
                //: "--";



            //*********************************************************************************

            deviceMeasurements = data.MeasurementsPackage.Last();
            temperature = deviceMeasurements.Measurements
                .FirstOrDefault(m => m.MeasurementType == "Temperatue");

            humidity = deviceMeasurements.Measurements
                .FirstOrDefault(m => m.MeasurementType == "Humidity");


            DeviceIdText2.Text = deviceMeasurements.DeviceId;

            if (temperature == null || humidity == null)
            {
                return;

            }

            LocationText2.Text = $"Location: {deviceMeasurements.Location}";

            TemperatureText2.Text = temperature.Value.ToString();

            //? $"{data.Temperature.Value.ToString("0.0", culture)} {data.TemperatureUnit}"
            //: "--";

            HumidityText2.Text = humidity.Value.ToString();

            //? $"{data.Humidity.Value.ToString("0.0", culture)} {data.HumidityUnit}"
            //: "--";




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
