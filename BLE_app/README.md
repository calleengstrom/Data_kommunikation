# BLE App

A Windows desktop app for configuring ESP32 devices over Bluetooth Low Energy (BLE).
It lets you update device ID, location, Wi-Fi, and MQTT settings.

## Requirements

- Windows with Bluetooth Low Energy support
- .NET 8 SDK
- An ESP32 advertising a device name that starts with `ESP32`
- ESP32 firmware exposing the configuration GATT service and characteristics described below

## Build and run

Open `BLE_app.slnx` in Visual Studio, or run the app from a terminal:

```powershell
dotnet run --project BLE_app/BLE_app.csproj
```

The project targets `net8.0-windows10.0.19041.0`. Restore and build it with:

```powershell
dotnet restore BLE_app.slnx
dotnet build BLE_app.slnx
```

## Configure a device

1. Select **Scan** and choose a discovered ESP32.
2. Select **Connect**. The app reads the device's current settings.
3. Edit device, Wi-Fi, or MQTT values. Password fields are blank initially; leave them blank to keep the existing passwords.
4. Select **Save Settings** to send the changes, or **Cancel** to discard them.

The device name must begin with `ESP32` (case-insensitive). Bluetooth must be enabled on the PC, and the ESP32 must be in range and advertising.

## ESP32 BLE interface

The app looks for a GATT service whose UUID ends in `0001`. It uses the service UUID's preceding portion as the prefix for characteristic UUIDs. Characteristic values are read and written as UTF-8 text.

| UUID suffix | Purpose |
| --- | --- |
| `0002` | Device ID |
| `0003` | Wi-Fi SSID |
| `0004` | Wi-Fi password (write when provided) |
| `0005` | MQTT broker address |
| `0006` | Device location |
| `0007` | MQTT port |
| `0008` | MQTT username |
| `0009` | MQTT password (write when provided) |
| `0010` | MQTT publish topic |
| `0011` | Command (`SAVE` or `CANCEL`) |
| `0012` | Command status; saving expects `SAVE_REQUESTED` |

The service must provide the characteristics the app uses to load settings and issue commands. Password values are not read by the app; a password is only written when a non-empty value is entered.
