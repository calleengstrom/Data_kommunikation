using System;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Models
{
    public record RequestModel(
            string DeviceId,
            string Location,
            Measurement[] Measurements
        ) {}
       


}
