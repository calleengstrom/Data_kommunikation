#include <Arduino.h>
#include <WiFi.h>
#include <DHT.h>
#include <ESP32MQTTClient.h>
#include <math.h>
#include <esp_idf_version.h>

namespace NetWork_n {
  constexpr char WIFI_SSID[] = "*****";
  constexpr char WIFI_PASS[] = "*****";
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

  float getTemperature() {
    return dht.readTemperature();
  }

  float getHumidity() {
    return dht.readHumidity();
  }

  void printTemperature() {
    Serial.println("Temp  : " + String(getTemperature(), 2) + " C");
  }

  void printHumidity() {
    Serial.println("Humidity : " + String(getHumidity(), 2) + " %");
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

  void mqttPub() {
    if (!mqttClient.isConnected()) {
      return;
    }

    unsigned long currentTime = millis();

    if (currentTime - lastPubTime < PUB_INTERVAL) {
    return;
    }

    lastPubTime = currentTime;

    float temperature = DHT11_n::getTemperature();
    
    if(isnan(temperature)){
      return;
    }

    char payload[64];
    snprintf(payload, sizeof(payload),"%s;%.1f" ,MQTT_USERNAME, temperature);

    mqttClient.publish(MQTT_TOPIC,payload,0,false);
  }

};


void onMqttConnect(esp_mqtt_client_handle_t client){
  // Mqtt_n::mqttClient.publish("todo");
  Serial.println("Connected to broker");
}

#if ESP_IDF_VERSION < ESP_IDF_VERSION_VAL(5,0,0)
  esp_err_t handleMQTT(esp_mqtt_event_handle_t event){
    Mqtt_n::mqttClient.onEventCallback(event);
    return ESP_OK; 
  }
#else
  void handleMQTT(void *handler_args,esp_event_base_t base, int32_t event_id,void *event_data){
    auto event = static_cast<esp_mqtt_event_handle_t>(event_data);
    Mqtt_n::mqttClient.onEventCallback(event);
  }
#endif


void setup() {
  Serial.begin(115200);
  delay(2000);
  DHT11_n::initDHT();
  NetWork_n::initWifi();
  Mqtt_n::initMqtt();
}

void loop() {
  Mqtt_n::mqttPub();
  delay(10);
 
}
