using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaEdgeState
    {
        [JsonPropertyName("edgeId")]
        public required string EdgeId { get; set; }

        [JsonPropertyName("sequenceId")]
        public uint SequenceId { get; set; }

        [JsonPropertyName("released")]
        public bool Released { get; set; }
    }
}
