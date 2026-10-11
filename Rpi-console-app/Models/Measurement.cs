using System;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Models
{
    public record Measurement(
         string MeasuremnetType,
         decimal Value,
         string Unit
        )
    {}

}
