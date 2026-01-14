using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaHeader
    {
        [JsonPropertyName("headerId")]
        public uint HeaderId { get; set; }

        [JsonPropertyName("timestamp")]
        public required string Timestamp { get; set; } // ISO8601

        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.0";

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = "Movu";

        [JsonPropertyName("serialNumber")]
        public required string SerialNumber { get; set; }
    }
}
