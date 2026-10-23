using System;
using System.Collections.Generic;
using System.Text;


namespace AzureDashboard.Models
{
    public record DeviceModel(
            DateTimeOffset ReceivedAt,
            string DeviceId,
            string Location,
            Measurement[] Measurements
        ) {}
       


}
