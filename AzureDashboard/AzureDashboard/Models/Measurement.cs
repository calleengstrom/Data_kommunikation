using System;
using System.Collections.Generic;
using System.Text;


namespace AzureDashboard.Models
{
    public record Measurement(
         string MeasurementType,
         decimal Value,
         string Unit
        )
    {}

}
