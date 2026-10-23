# RPi Console App

A small .NET console application that connects an MQTT gateway to Azure IoT Hub and forwards data from local sensors or devices.

## Features

- Reads environment-based configuration
- Connects to an MQTT broker
- Collects measurements from the gateway cache
- Sends telemetry to Azure IoT Hub on a timed interval
- Stops cleanly with Ctrl+C

## Prerequisites

- .NET 10 SDK
- An MQTT broker reachable by the app
- An Azure IoT Hub device connection string

## Configuration

Set the following environment variables before starting the app:

```bash
export AZURE_CONNECTION_STRING="<your-iot-hub-device-connection-string>"
export MQTT_HOST="localhost"
export MQTT_USERNAME=""
export MQTT_PASSWORD=""
export MQTT_TOPIC=""
```

On Windows PowerShell, use:

```powershell
$env:AZURE_CONNECTION_STRING = "<your-iot-hub-device-connection-string>"
$env:MQTT_HOST = "localhost"
$env:MQTT_USERNAME = ""
$env:MQTT_PASSWORD = ""
$env:MQTT_TOPIC = ""
```

The app uses the default MQTT port 1883.

## Run

```bash
dotnet run --project "Rpi-console-app/Rpi-console-app.csproj"
```

## Notes

The app expects the Azure connection string to be set. If it is missing, it exits immediately with a message telling you to configure it first.
