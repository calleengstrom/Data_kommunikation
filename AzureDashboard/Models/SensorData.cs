using System;

namespace AzureDashboard.Models
{
    public class SensorData
    {
        public string GatewayId { get; set; } = "--";
        public string DeviceId { get; set; } = "--";
        public string Location { get; set; } = "--";

        public double? Temperature { get; set; }
        public string TemperatureUnit { get; set; } = "°C";

        public double? Humidity { get; set; }
        public string HumidityUnit { get; set; } = "%";

        public DateTimeOffset SentAt { get; set; }
    }
}
