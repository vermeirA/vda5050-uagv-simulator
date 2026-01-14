using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaState : VdaHeader
    {
        [JsonPropertyName("orderId")]
        public required string OrderId { get; set; }

        [JsonPropertyName("orderUpdateId")]
        public uint OrderUpdateId { get; set; }

        [JsonPropertyName("lastNodeId")]
        public required string LastNodeId { get; set; }

        [JsonPropertyName("lastNodeSequenceId")]
        public uint LastNodeSequenceId { get; set; }

        [JsonPropertyName("nodeStates")]
        public List<VdaNodeState> NodeStates { get; set; } = new();

        [JsonPropertyName("edgeStates")]
        public List<VdaEdgeState> EdgeStates { get; set; } = new();

        [JsonPropertyName("operatingMode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public VdaOperatingMode OperatingMode { get; set; }

        [JsonPropertyName("driving")]
        public bool Driving { get; set; }

        [JsonPropertyName("agvPosition")]
        public required VdaPosition AgvPosition { get; set; }

        [JsonPropertyName("errors")]
        public List<VdaError> Errors { get; set; } = new();
    }
}
