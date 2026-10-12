using System;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Models
{
    public record GatewayData
    (
        string GatewayId,
        DateTimeOffset? SentAt,
        List<RequestModel> MeasurementsPackage
        ){}
}

