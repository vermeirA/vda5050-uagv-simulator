using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaNodeState
    {
        [JsonPropertyName("nodeId")]
        public required string NodeId { get; set; }

        [JsonPropertyName("sequenceId")]
        public uint SequenceId { get; set; }

        [JsonPropertyName("released")]
        public bool Released { get; set; }

        [JsonPropertyName("nodePosition")]
        public required VdaPosition NodePosition { get; set; }
    }
}
