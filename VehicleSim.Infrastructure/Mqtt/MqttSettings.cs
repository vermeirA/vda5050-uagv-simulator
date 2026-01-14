using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleSim.Infrastructure.Mqtt
{
    public class MqttSettings
    {
        public required string BrokerIp { get; set; } 
        public required int Port { get; set; } 
        public required string TopicPrefix { get; set; }
        public required MqttTopics Topics { get; set; }
    }

    public class MqttTopics
    {
        public required string OrderTopic { get; set; }
        public required string StateTopic { get; set; }
        public required string ConnectionTopic { get; set; }
        public required string VisualizationTopic { get; set; }
    }
}
