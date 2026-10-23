# IoT Climate Monitoring System

A distributed IoT project for monitoring temperature and humidity using ESP32 devices, a Raspberry Pi gateway, Azure IoT Hub, and a Windows desktop application.

The system demonstrates communication between embedded devices, cloud services, and desktop applications using MQTT, BLE, and Azure.

## System Overview

**ESP32 Sensors → MQTT Broker → Raspberry Pi Gateway → Azure IoT Hub → Windows Dashboard**

ESP32 devices collect environmental measurements and publish them over MQTT. A Raspberry Pi receives and processes the data before forwarding it to Azure IoT Hub, where a Windows desktop application retrieves and displays the measurements.

## Components

### ESP32 – Arduino
- Reads temperature and humidity from DHT11 sensors.
- Publishes measurements using MQTT over Wi-Fi.
- Supports BLE configuration of device and network settings.

### BLE Configuration App
- Windows desktop application built with C# and WinUI 3.
- Discovers and connects to ESP32 devices via Bluetooth Low Energy.
- Configures device settings wirelessly.

### Raspberry Pi Gateway
- .NET console application running on Raspberry Pi.
- Uses Mosquitto as the MQTT broker.
- Receives and processes sensor measurements.
- Forwards data to Azure IoT Hub.
- Runs as a background service using systemd.

### Azure Dashboard
- Windows desktop application built with C# and WinUI 3.
- Receives telemetry through Azure Event Hubs.
- Displays temperature, humidity, device information, and timestamps.

## Technologies

- **Embedded:** ESP32, Arduino, C/C++
- **Backend:** C#, .NET
- **Desktop:** WinUI 3
- **Protocols:** MQTT, BLE, HTTPS
- **Cloud:** Azure IoT Hub, Azure Event Hubs
- **Platform:** Raspberry Pi, Linux, Windows
- **Data Format:** JSON
- **Version Control:** Git, GitHub

## Repository Structure

- `Esp32/` – ESP32 firmware and sensor communication
- `BLE_app/` – BLE device configuration application
- `Rpi-console-app/` – Raspberry Pi MQTT-to-Azure gateway
- `AzureDashboard/` – Windows telemetry dashboard

## Getting Started

### 1. ESP32 Firmware
1. Open the project in `Esp32/` using Arduino IDE.
2. Install the required ESP32 board support and libraries.
3. Upload the firmware to both ESP32 devices.
4. Configure the devices using the BLE Configuration App.

### 2. Raspberry Pi Gateway
1. Install and configure the Mosquitto MQTT broker on the Raspberry Pi.
2. Open the project in `Rpi-console-app/` and configure the MQTT and Azure IoT Hub connections.
3. Build and publish the .NET application for Raspberry Pi (Linux ARM64).
4. Run the application manually or configure it as a systemd service for automatic startup.

### 3. BLE Configuration App
1. Open the solution in `BLE_app/` using Visual Studio on Windows.
2. Build and launch the application.
3. Scan for available ESP32 devices and connect to one at a time.
4. Configure Wi-Fi, MQTT, and device settings.
5. Synchronize the configuration and restart the ESP32 if required.

### 4. Azure Dashboard
1. Open the solution in `AzureDashboard/` using Visual Studio on Windows.
2. Configure the Azure Event Hubs connection.
3. Build and launch the application.
4. View incoming sensor measurements in real time.

**Note:** Wi-Fi passwords, MQTT credentials, and Azure connection strings must be configured locally and are not included in this repository.

## Project Purpose

This project was developed as part of the Embedded IoT Software Development program at Nackademin.

The goal is to explore embedded programming, wireless communication, system integration, and cloud-based IoT solutions through a complete end-to-end system.

## License

Educational project developed for learning and demonstration purposes.