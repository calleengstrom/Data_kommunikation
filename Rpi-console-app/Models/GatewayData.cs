using System;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Models
{
    public record GatewayData
    (
        string DeviceId,
        DateTimeOffset? SentAt,
        List<RequestModel> Measurements
        ){}
}