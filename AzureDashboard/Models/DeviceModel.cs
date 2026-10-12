using System;
using System.Collections.Generic;
using System.Text;


namespace AzureDashboard.Models
{
    public record DeviceModel(
            string DeviceId,
            string Location,
            Measurement[] Measurements
        ) {}
       


}
