using System;
using System.Collections.Generic;

namespace AzureDashboard.Models
{
    public record SensorData
    (
        string GatewayId,
        DateTimeOffset SentAt,
        List<DeviceModel> MeasurementsPackage
        )
    { }
}
