# ESP32 Environmental Sensor

This project is a small ESP32 sketch that reads temperature and humidity from a DHT11 sensor, stores configuration in flash memory, and publishes the readings as JSON over MQTT.

## Features

- Reads temperature and humidity from a DHT11 sensor
- Connects to Wi-Fi using saved credentials
- Configures device settings through BLE
- Saves Wi-Fi, MQTT, and device settings in ESP32 Preferences
- Publishes JSON payloads to an MQTT broker

## Hardware

- ESP32 development board
- DHT11 sensor
- Jumper wires
- 3.3V power supply or USB connection

## Wiring

Connect the DHT11 sensor to the ESP32 as follows:

- DHT data pin -> GPIO 4
- VCC -> 3.3V
- GND -> GND

## Setup

1. Open `sketch_oct7a.ino` in the Arduino IDE or VS Code with the ESP32 board support installed.
2. Install the required libraries:
   - Arduino
   - WiFi
   - DHT
   - ESP32MQTTClient
3. Select your ESP32 board and serial port.
4. Upload the sketch.
5. Use the BLE configuration service to set:
   - Wi-Fi SSID and password
   - Device ID
   - MQTT host, port, username, password, and topic
   - Location
6. Save the settings and restart the device.

## Example payload

```json
{
  "deviceId": "esp_temp_XX:XX:XX:XX:XX:XX",
  "location": "Kitchen",
  "measurements": [
    { "measurementType": "temperature", "value": 22.4, "unit": "°C" },
    { "measurementType": "humidity", "value": 48.2, "unit": "%" }
  ]
}
```

## Notes

- BLE is used for initial configuration and updates.
- Settings are stored persistently in the ESP32 flash memory.
- The device publishes readings periodically every few seconds.

## License

This project is provided as-is for educational and personal use.
