using System;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Models
{
    public record RequestModel(
            DateTimeOffset ReceivedAt,
            string DeviceId,
            string Location,
            Measurement[] Measurements
        ) {}
       


}
