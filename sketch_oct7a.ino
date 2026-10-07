#include <Arduino.h>
#include <WiFi.h>
#include <DHT.h>

constexpr char WIFI_SSID[] = "xxxx";
constexpr char WIFI_PASS[] = "xxxx";

bool wasConnected = false;
unsigned long lastStatusMessage = 0;

#define DHT_TYPE DHT11
#define DHT_PIN 4


DHT dht(DHT_PIN, DHT_TYPE);

void setup() {
  Serial.begin(115200);
  delay(2000);

  dht.begin();

  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(WIFI_SSID, WIFI_PASS);



  Serial.print("Connecting to ");
  Serial.println(WIFI_SSID);
}

void loop() {
  const bool isConnected = WiFi.status() == WL_CONNECTED;

  if (isConnected && !wasConnected) {
    Serial.println("WIFI connected!");

    Serial.print("IPv4 Address: ");
    Serial.println(WiFi.localIP());

    Serial.print("Signal Strength: ");
    Serial.print(WiFi.RSSI());
    Serial.println(" dBm");
  }

  if (!isConnected && wasConnected) {
    Serial.println("Reconnecting to WiFi...");
  }

  if (!isConnected && millis() - lastStatusMessage >= 5000) {
    lastStatusMessage = millis();
    Serial.println("Awaiting WiFi connection...");
  }

  wasConnected = isConnected;

  delay(100);


  float temperature = dht.readTemperature();
  float humidity = dht.readHumidity();

  if (isnan(temperature) || isnan(humidity)) {
    Serial.println("Unable to read data from DHT-11.");
    delay(2000);
    return;
  }

  String data = String(temperature, 2) + ";" + String(humidity, 2);
  Serial.println("Temp  : "+String(temperature, 2)+" C");
  Serial.println("Humidity : "+ String(humidity, 2) + " %");

  delay(5000);
}
