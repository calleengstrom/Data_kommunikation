


#include <Arduino.h>
#include <WiFi.h>
#include <DHT.h>
#include <ESP32MQTTClient.h>
#include <math.h>
#include <esp_idf_version.h>
#include <Preferences.h>
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>



class DeviceSettings {

public:
  String wifiSSID;
  String wifiPassword;

  String deviceId;
  String location;

  String mqttHost;
  uint16_t mqttPort = 1883;
  String mqttUsername;
  String mqttPassword;
  String mqttTopic;

  void loadDeviceSettings() {
    Preferences preferences;
    preferences.begin("device-config", true);

    wifiSSID = preferences.getString("wifiSSID", "");
    wifiPassword = preferences.getString("wifiPassword", "");

    deviceId = preferences.getString("deviceId", "esp_temp_" + WiFi.macAddress());
    location = preferences.getString("location", "");

    mqttHost = preferences.getString("mqttHost", "");
    mqttPort = preferences.getUShort("mqttPort", 1883);
    mqttUsername = preferences.getString("mqttUsername", "");
    mqttPassword = preferences.getString("mqttPassword", "");
    mqttTopic = preferences.getString("mqttTopic", "");

    preferences.end();
  }

  void saveDeviceSettings() {
    Preferences preferences;
    preferences.begin("device-config", false);

    preferences.putString("wifiSSID", wifiSSID);
    preferences.putString("wifiPassword", wifiPassword);

    preferences.putString("deviceId", deviceId);
    preferences.putString("location", location);

    preferences.putString("mqttHost", mqttHost);
    preferences.putUShort("mqttPort", mqttPort);
    preferences.putString("mqttUsername", mqttUsername);
    preferences.putString("mqttPassword", mqttPassword);
    preferences.putString("mqttTopic", mqttTopic);

    preferences.end();
  }


  void printDeviceSettings() const {
    Serial.println("\n========== DEVICE SETTINGS ==========");

    Serial.println("WiFi SSID     : " + wifiSSID);
    Serial.println("WiFi Password : [HIDDEN]");

    Serial.println("Device ID     : " + deviceId);
    Serial.println("Location      : " + location);

    Serial.println("MQTT Host     : " + mqttHost);
    Serial.println("MQTT Port     : " + String(mqttPort));
    Serial.println("MQTT Username : " + mqttUsername);
    Serial.println("MQTT Password : [HIDDEN]");
    Serial.println("MQTT Topic    : " + mqttTopic);

    Serial.println("=====================================\n");
  }

  bool saveIsValid() const {
    return !wifiSSID.isEmpty()
           && !deviceId.isEmpty()
           && !mqttHost.isEmpty()
           && mqttPort > 0
           && !mqttTopic.isEmpty();
  }
};



#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>


DeviceSettings activeSettings;
DeviceSettings pendingSettings;

namespace BLE_n {

constexpr char SERVICE_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0001";

constexpr char DEVICE_ID_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0002";
constexpr char WIFI_SSID_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0003";
constexpr char WIFI_PASSWORD_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0004";
constexpr char MQTT_HOST_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0005";
constexpr char LOCATION_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0006";
constexpr char MQTT_PORT_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0007";
constexpr char MQTT_USERNAME_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0008";
constexpr char MQTT_PASSWORD_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0009";
constexpr char MQTT_TOPIC_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0010";
constexpr char COMMAND_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0011";
constexpr char STATUS_UUID[] = "18e4b47a-d691-41ab-b37a-28a37dbe0012";

BLECharacteristic* statusChars = nullptr;

bool settingsChanged = false;

void setStatus(const String& status) {
  if (statusChars != nullptr) {
    statusChars->setValue(status.c_str());
    statusChars->notify();
  }
  Serial.println("BLE Status: " + status);
}

// Gemensam callback för alla String-inställningar
class StringCallbacks : public BLECharacteristicCallbacks {
private:
  String DeviceSettings::*field;
  bool readable;

public:
  StringCallbacks(String DeviceSettings::*member, bool allowRead = true)
    : field(member), readable(allowRead) {}

  void onRead(BLECharacteristic* characteristic) override {
    if (readable) {
      characteristic->setValue((activeSettings.*field).c_str());
    }
  }

  void onWrite(BLECharacteristic* characteristic) override {
    String value = String(characteristic->getValue().c_str());

    // Uppdatera bara det aktuella fältet i RAM
    pendingSettings.*field = value;

    Serial.println("BLE: Setting updated in RAM");
  }
};

// MQTT-porten är uint16_t, inte String
class PortCallbacks : public BLECharacteristicCallbacks {
public:
  void onRead(BLECharacteristic* characteristic) override {
    characteristic->setValue(
      String(activeSettings.mqttPort).c_str());
  }

  void onWrite(BLECharacteristic* characteristic) override {
    String value = String(characteristic->getValue().c_str());

    // Acceptera bara numeriska portnummer 1-65535
    if (value.isEmpty()) {
      setStatus("INVALID_PORT");
      return;
    }

    for (unsigned int i = 0; i < value.length(); i++) {
      if (!isDigit(value[i])) {
        setStatus("INVALID_PORT");
        return;
      }
    }

    unsigned long port = value.toInt();

    if (port == 0 || port > 65535) {
      setStatus("INVALID_PORT");
      return;
    }

    pendingSettings.mqttPort = static_cast<uint16_t>(port);
  }
};

// Hanterar SAVE och CANCEL
class CommandCallbacks : public BLECharacteristicCallbacks {
public:
  void onWrite(BLECharacteristic* characteristic) override {
    String command = String(characteristic->getValue().c_str());
    command.trim();

    if (command == "SAVE") {

      if (!pendingSettings.saveIsValid()) {
        setStatus("INVALID_SETTINGS");
        return;
      }

      // Spara i Preferences
      pendingSettings.saveDeviceSettings();

      // Aktivera den nya konfigurationen i RAM
      activeSettings = pendingSettings;

      settingsChanged = true;

      // Med nuvarande void-metod kan vi inte
      // bekräfta att varje skrivning lyckades.
      setStatus("SAVE_REQUESTED");


    }

    else if (command == "CANCEL") {
      pendingSettings = activeSettings;
      setStatus("CANCELLED");
    }

    else {
      setStatus("UNKNOWN_COMMAND");
    }
  }
};

// Skapar en characteristic för ett String-fält
BLECharacteristic* addStringCharacteristic(
  BLEService* service,
  const char* uuid,
  String DeviceSettings::*field,
  bool readable = true) {

  uint32_t properties = BLECharacteristic::PROPERTY_WRITE;

  if (readable) {
    properties |= BLECharacteristic::PROPERTY_READ;
  }

  BLECharacteristic* characteristic =
    service->createCharacteristic(uuid, properties);

  characteristic->setCallbacks(
    new StringCallbacks(field, readable));

  return characteristic;
}

// Initiera BLE
void BLE_init() {

  pendingSettings = activeSettings;

  BLEDevice::init("ESP32-Config");

  BLEServer* server = BLEDevice::createServer();
  BLEService* service = server->createService(BLEUUID(String(SERVICE_UUID)), 40);

  // Device
  addStringCharacteristic(
    service, DEVICE_ID_UUID, &DeviceSettings::deviceId);

  addStringCharacteristic(
    service, LOCATION_UUID, &DeviceSettings::location);

  // Wi-Fi
  addStringCharacteristic(
    service, WIFI_SSID_UUID, &DeviceSettings::wifiSSID);

  addStringCharacteristic(
    service, WIFI_PASSWORD_UUID,
    &DeviceSettings::wifiPassword, false);

  // MQTT
  addStringCharacteristic(
    service, MQTT_HOST_UUID, &DeviceSettings::mqttHost);

  addStringCharacteristic(
    service, MQTT_USERNAME_UUID, &DeviceSettings::mqttUsername);

  addStringCharacteristic(
    service, MQTT_PASSWORD_UUID,
    &DeviceSettings::mqttPassword, false);

  addStringCharacteristic(
    service, MQTT_TOPIC_UUID, &DeviceSettings::mqttTopic);

  // MQTT Port
  BLECharacteristic* portChars = service->createCharacteristic(
    MQTT_PORT_UUID,
    BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_WRITE);

  portChars->setCallbacks(new PortCallbacks());

  // SAVE / CANCEL
  BLECharacteristic* commandChars = service->createCharacteristic(
    COMMAND_UUID,
    BLECharacteristic::PROPERTY_WRITE);

  commandChars->setCallbacks(new CommandCallbacks());

  // Status
  statusChars = service->createCharacteristic(
    STATUS_UUID,
    BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);

  statusChars->addDescriptor(new BLE2902());
  statusChars->setValue("READY");

  // Starta service
  service->start();

  // Advertising
  BLEAdvertising* advertising = BLEDevice::getAdvertising();
  advertising->addServiceUUID(SERVICE_UUID);
  advertising->setScanResponse(true);

  BLEDevice::startAdvertising();

  Serial.println("BLE configuration server started.");
  Serial.println("Waiting for BLE client...");
}
}



class Measurement_t {
private:
  const String measurementType;
  const float value;
  const String unit;

public:
  Measurement_t() = default;
  Measurement_t(String const measurementType, float const value, String const unit)
    : measurementType(measurementType), value(value), unit(unit) {}

  String getMeasurementType() const {
    return measurementType;
  }

  float getValue() const {
    return value;
  }
  String getUnit() const {
    return unit;
  }


  bool isValid() {
    return isfinite(value);
  }
};


class Payload_t {

private:
  const String deviceId;
  const Measurement_t measurements[2];
  const String location;

public:

  Payload_t() = default;

  Payload_t(String const deviceId, Measurement_t const measurements[2], String const location)
    : deviceId(deviceId), measurements{ measurements[0], measurements[1] }, location(location) {}


  String getLocation() const {
    return location;
  }

  const Measurement_t* getMeasurements() const {
    return measurements;
  }

  String getDeviceId() const {
    return deviceId;
  }
};


namespace NetWork_n {
bool wasConnected = false;
void initWifi() {

  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(activeSettings.wifiSSID.c_str(), activeSettings.wifiPassword.c_str());
  Serial.print("Connecting to ");
  Serial.println(activeSettings.wifiSSID);
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(500);
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println();
    Serial.println("WIFI connected!");

    Serial.print("IPv4 Address: ");
    Serial.println(WiFi.localIP());
  }
}
}

namespace DHT11_n {
#define DHT_TYPE DHT11
#define DHT_PIN 4
DHT dht(DHT_PIN, DHT_TYPE);

void initDHT() {
  dht.begin();
  delay(1000);
}

float getTemperatureValue() {
  return dht.readTemperature();
}
String getTemperatureUnit() {
  return "°C";
}


float getHumidityValue() {
  return dht.readHumidity();
}

String getHumidityUnit() {
  return "%";
}

void printTemperature() {
  Serial.println("Temp  : " + String(getTemperatureValue(), 2) + "°C");
}

void printHumidity() {
  Serial.println("Humidity : " + String(getHumidityValue(), 2) + "%");
}
}

namespace Mqtt_n {

unsigned long lastPubTime = 0;
constexpr unsigned long PUB_INTERVAL = 5000;


ESP32MQTTClient mqttClient;

void initMqtt() {
  mqttClient.setURL(activeSettings.mqttHost.c_str(), activeSettings.mqttPort, activeSettings.mqttUsername.c_str(), activeSettings.mqttPassword.c_str());
  mqttClient.setMqttClientName(activeSettings.mqttUsername.c_str());
  mqttClient.loopStart();
}

void mqttPub(String payload) {
  if (!mqttClient.isConnected()) {
    return;
  }

  unsigned long currentTime = millis();

  if (currentTime - lastPubTime < PUB_INTERVAL) {
    return;
  }

  lastPubTime = currentTime;

  mqttClient.publish(activeSettings.mqttTopic.c_str(), payload.c_str(), 0, false);
}

};


void onMqttConnect(esp_mqtt_client_handle_t client) {
  // Mqtt_n::mqttClient.publish("todo");
  Serial.println("Connected to broker");
}

#if ESP_IDF_VERSION < ESP_IDF_VERSION_VAL(5, 0, 0)
esp_err_t handleMQTT(esp_mqtt_event_handle_t event) {
  Mqtt_n::mqttClient.onEventCallback(event);
  return ESP_OK;
}
#else
void handleMQTT(void* handler_args, esp_event_base_t base, int32_t event_id, void* event_data) {
  auto event = static_cast<esp_mqtt_event_handle_t>(event_data);
  Mqtt_n::mqttClient.onEventCallback(event);
}
#endif



String jsonParser(const Payload_t& payload) {

  String json = "{";

  json += "\"deviceId\":\"" + payload.getDeviceId() + "\",";
  json += "\"location\":\"" + payload.getLocation() + "\",";
  json += "\"measurements\":[";

  for (int i = 0; i < 2; i++) {

    json += "{";
    json += "\"measurementType\":\"" + payload.getMeasurements()[i].getMeasurementType() + "\",";
    json += "\"value\":" + String(payload.getMeasurements()[i].getValue(), 1) + ",";
    json += "\"unit\":\"" + payload.getMeasurements()[i].getUnit() + "\"";
    json += "}";

    if (i < 1) {
      json += ",";
    }
  }

  json += "]";
  json += "}";

  return json;
}



void setup() {
  Serial.begin(115200);
  delay(2000);
  activeSettings.loadDeviceSettings();
  activeSettings.printDeviceSettings();
  BLE_n::BLE_init();


  if (!activeSettings.wifiSSID.isEmpty() && !activeSettings.wifiPassword.isEmpty()) {
    DHT11_n::initDHT();
    NetWork_n::initWifi();
    Mqtt_n::initMqtt();
  }
}

void loop() {
  if (!activeSettings.wifiSSID.isEmpty() && !activeSettings.wifiPassword.isEmpty()) {
    Measurement_t temperature("Temperatue", DHT11_n::getTemperatureValue(), DHT11_n::getTemperatureUnit());
    Measurement_t humidity("Humidity", DHT11_n::getHumidityValue(), DHT11_n::getHumidityUnit());
    if (temperature.isValid() && humidity.isValid()) {
      Measurement_t measurements[] = { temperature, humidity };
      Payload_t payload(activeSettings.deviceId, measurements, activeSettings.location);
      String jsonPayload = jsonParser(payload);
      Mqtt_n::mqttPub(jsonPayload);
    }
  }

  if (BLE_n::settingsChanged) {
    Serial.println("New settings -> Restarting");
    delay(1500);
    ESP.restart();
  }

  delay(5000);
}
