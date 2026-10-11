using MQTTnet;
using MQTTnet.Protocol;
using RpiApp.Config;
using RpiApp.Models;
using System.Buffers;
using System.Text.Json;

namespace RpiApp.Services
{
    public class MqttService(GatewayOptions options, MeasurementCache measurementCache) : IDisposable
    {
        private const string DataTopic = "climate/data";


        private readonly IMqttClient _mqttClient = new MqttClientFactory().CreateMqttClient();

        private readonly JsonSerializerOptions _jsonOptoions = new(JsonSerializerDefaults.Web);

        public bool IsConneted => _mqttClient.IsConnected;
        private int _subscriptionRequred = 1;
        public async Task RunAsync(CancellationToken ct)
        {
            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"{options.GatewayName}-gateway")
                .WithTcpServer(options.MqttHost, options.MqttPort)
                .WithCredentials(options.MqttUsername,options.MqttPassword)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .Build();


            _mqttClient.ConnectedAsync += _ =>
            {
                Interlocked.Exchange(ref _subscriptionRequred, 1);
                return Task.CompletedTask;
            };

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!_mqttClient.IsConnected)
                    {
                        Console.WriteLine($"Conneting to Mqtt: {options.MqttHost}");
                        await _mqttClient.ConnectAsync(clientOptions, ct);
                    }

                    if(Volatile.Read(ref _subscriptionRequred) == 1)
                    {
                        await SubscribeAsync(ct);
                        Interlocked.Exchange(ref _subscriptionRequred, 0);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Mqtt connection : {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }


        private async Task SubscribeAsync(CancellationToken cancellationToken)
        {
            var factory = new MqttClientFactory();

            var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(filter => filter
                    .WithTopic(DataTopic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .Build();

            var result = await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);

            if (result.Items.Any(item => (int)item.ResultCode >= 128))
                throw new InvalidOperationException("MQTT subscription was rejected.");

            Console.WriteLine($"Subscribed to {DataTopic}.");
        }
        private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var payload = e.ApplicationMessage.Payload.ToArray();
                var data = JsonSerializer.Deserialize<RequestModel>(payload, _jsonOptoions);

                if (data == null || string.IsNullOrWhiteSpace(data.DeviceId) || data.Measurements is not { Length: > 0})
                {
                    Console.WriteLine("No data found");
                    return Task.CompletedTask;
                }
                Console.WriteLine("Data from MQTT Topic : ");
                Console.WriteLine(data);
                foreach(var d in data.Measurements)
                {
                    Console.WriteLine($"Measurement type : {d.MeasurementType} Value : {d.Value} Unit : {d.Unit} ");

                }
                measurementCache.UpdateCacheMeasuremnets(data);


            }
            catch (JsonException)
            {
                Console.WriteLine($"Ignored invalid json on {e.ApplicationMessage.Topic}");
            }
            catch (Exception ex) { }



            return Task.CompletedTask;
        }



        public void Dispose()
        {
            _mqttClient.Dispose();
        }
    }
}
