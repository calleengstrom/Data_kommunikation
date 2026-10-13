using System;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Config
{
    public record GatewayOptions
    (
        string GatewayName,
        string MqttHost,
        int MqttPort,
        string AzureConnectionString,        
        TimeSpan AzureSendInterval,

        string MqttUsername,
        string MqttPassword,
        string MqttTopic
    )
    {}
}
