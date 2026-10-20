
using RpiApp.Config;
using RpiApp.Services;

var connectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTION_STRING");


if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Set IOTHUB_DEVICE_CONNECTION_STRING before starting.");
    return;
}

var options = new GatewayOptions(
    GatewayName: "crpi",
    MqttHost: Environment.GetEnvironmentVariable("MQTT_HOST") ?? "localhost",
    MqttPort: 1883,
    AzureConnectionString: connectionString,
    AzureSendInterval: TimeSpan.FromSeconds(30),
    MqttUsername: Environment.GetEnvironmentVariable("MQTT_USERNAME") ?? "",
    MqttPassword: Environment.GetEnvironmentVariable("MQTT_PASSWORD") ?? "",
    MqttTopic: Environment.GetEnvironmentVariable("MQTT_TOPIC") ?? "");

using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};

var measurementCache = new MeasurementCache();

using var mqttService = new MqttService(options, measurementCache);
using var azureService = new AzureIotHubService(options, mqttService, measurementCache);

Console.WriteLine("Starting gateway. Press Ctrl+C to stop.");



var mqttTask = mqttService.RunAsync(shutdown.Token);
var azureTask = azureService.RunAsync(shutdown.Token);

try
{
    // Stop the other service if either service exits or fails.
    await Task.WhenAny(mqttTask, azureTask);
    shutdown.Cancel();

    await Task.WhenAll(mqttTask, azureTask);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
    Console.WriteLine("Gateway stopped.");
}
catch (Exception ex)
{
    Console.WriteLine($"Gateway failed: {ex.Message}");
}
finally
{
    await azureService.CloseAsync();
}
