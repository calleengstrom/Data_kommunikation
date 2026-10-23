# Azure Sensor Dashboard

A Windows desktop app built with WinUI 3 and .NET. It reads sensor events from Azure Event Hubs and shows the latest location, temperature, and humidity for each device.

## Requirements

- Windows 10 version 1809 (build 17763) or later
- .NET 10 SDK
- An Azure Event Hubs connection string with permission to receive events

## Configure and run

Set the `AZURE_EVENTHUB_CONNECTION_STRING` environment variable to a connection string for your Event Hub. In PowerShell, you can set it for the current terminal session:

```powershell
$env:AZURE_EVENTHUB_CONNECTION_STRING = "<your-event-hubs-connection-string>"
dotnet run --project .\AzureDashboard\AzureDashboard.csproj
```

The app reads this variable when it starts. Keep the connection string private and do not commit it to the repository.

Select **Connect** to begin receiving events, and **Disconnect** to stop. The dashboard displays the connection status, gateway ID, and last update time. Device cards are added as events arrive and updated by device ID.

## Event format

Each event body should be JSON with a gateway ID, a send timestamp, and one or more device measurements. For example:

```json
{
  "GatewayId": "gateway-01",
  "SentAt": "2026-10-23T12:30:00Z",
  "MeasurementsPackage": [
    {
      "ReceivedAt": "2026-10-23T12:29:58Z",
      "DeviceId": "sensor-01",
      "Location": "Living room",
      "Measurements": [
        { "MeasurementType": "Temperature", "Value": 21.5, "Unit": "°C" },
        { "MeasurementType": "Humidity", "Value": 45, "Unit": "%" }
      ]
    }
  ]
}
```

The app matches measurement types `Temperature` and `Humidity` without case sensitivity. Events that are not valid JSON are ignored.
