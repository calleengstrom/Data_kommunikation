using Microsoft.Azure.Devices.Client;
using RpiApp.Config;
using RpiApp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RpiApp.Services
{
    public class AzureIotHubService(GatewayOptions options, MqttService mqttService, MeasurementCache measurementCache) : IDisposable
    {

        private readonly DeviceClient _client = DeviceClient.CreateFromConnectionString(options.AzureConnectionString, TransportType.Mqtt_WebSocket_Only);
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private CancellationToken _shutdownToken;

        public async Task RunAsync(CancellationToken ct)
        {
            _shutdownToken = ct;

            await _client.OpenAsync(ct);
            Console.WriteLine($"Connected to Azure Iot hub ");

            using var timer = new PeriodicTimer(options.AzureSendInterval);
           
            while (await timer.WaitForNextTickAsync(ct))
            {
                await SendPayloadAsync(ct);
            }
        }

        private async Task SendPayloadAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;


            var measuerments = measurementCache.GetAllCacheMeasurements();

            if (measuerments == null)
            {
                Console.WriteLine("No data in cache");
                return;
            }

            var payload = new GatewayData(options.GatewayName, now, measuerments);

            var jsonPayload = JsonSerializer.Serialize(payload);

            if(jsonPayload.Length > 240 * 1024)
                {
                    Console.WriteLine("Payload to large");
                    return;
                }

                using var message = new Message(Encoding.UTF8.GetBytes(jsonPayload))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));


                try
                {

                await _client.SendEventAsync(message,timeout.Token);

                    Console.WriteLine($"Message sent : {message}");

                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Error : {ex.Message}");
                }
            }


        public async Task CloseAsync()
        {
            try
            {
                await _client.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Azure close failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }

}
