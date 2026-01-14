using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaNode
    {
        [JsonPropertyName("nodeId")]
        public required string NodeId { get; set; }

        [JsonPropertyName("sequenceId")]
        public uint SequenceId { get; set; }

        [JsonPropertyName("nodePosition")]
        public required VdaPosition Position { get; set; }

        [JsonPropertyName("released")]
        public bool Released { get; set; } = true;
    }
}
