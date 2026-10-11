#include <Arduino.h>
#include <WiFi.h>
#include <DHT.h>
#include <ESP32MQTTClient.h>
#include <math.h>
#include <esp_idf_version.h>


namespace Device_n {
constexpr char deviceId[] = "esp_temp_04:B2:47:97:C5:48";
constexpr char location[] = "Office";
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
constexpr char WIFI_SSID[] = "xxxx";
constexpr char WIFI_PASS[] = "xxxx";
bool wasConnected = false;
void initWifi() {

  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  Serial.print("Connecting to ");
  Serial.println(WIFI_SSID);
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
constexpr char MQTT_HOST[] = "192.168.0.240";
constexpr uint16_t MQTT_PORT = 1883;
constexpr char MQTT_USERNAME[] = "esp_temp_001";
constexpr char MQTT_PASSWORD[] = "BytMig123!";
constexpr char MQTT_TOPIC[] = "/temp/1";

ESP32MQTTClient mqttClient;

void initMqtt() {
  mqttClient.setURL(MQTT_HOST, MQTT_PORT, MQTT_USERNAME, MQTT_PASSWORD);
  mqttClient.setMqttClientName(MQTT_USERNAME);
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

  mqttClient.publish(MQTT_TOPIC, payload.c_str(), 0, false);
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
  DHT11_n::initDHT();
  NetWork_n::initWifi();
  Mqtt_n::initMqtt();
}

void loop() {

  Measurement_t temperature("Temperatue", DHT11_n::getTemperatureValue(), DHT11_n::getTemperatureUnit());
  Measurement_t humidity("Humidity", DHT11_n::getHumidityValue(), DHT11_n::getHumidityUnit());
  if (temperature.isValid() && humidity.isValid()) {
    Measurement_t measurements[] = { temperature, humidity };
    Payload_t payload(Device_n::deviceId, measurements, Device_n::location);
    String jsonPayload = jsonParser(payload);
    Mqtt_n::mqttPub(jsonPayload);
  }


  delay(5000);
}
