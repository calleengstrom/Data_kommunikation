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

## Project Purpose

This project was developed as part of the Embedded IoT Software Development program at Nackademin.

The goal is to explore embedded programming, wireless communication, system integration, and cloud-based IoT solutions through a complete end-to-end system.

## License

Educational project developed for learning and demonstration purposes.