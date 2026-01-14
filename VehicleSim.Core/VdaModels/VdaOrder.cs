using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaOrder : VdaHeader
    {
        [JsonPropertyName("orderId")]
        public required string OrderId { get; set; }

        [JsonPropertyName("orderUpdateId")]
        public uint OrderUpdateId { get; set; }

        [JsonPropertyName("nodes")]
        public List<VdaNode> Nodes { get; set; } = new();

        [JsonPropertyName("edges")]
        public List<VdaEdge> Edges { get; set; } = new();
    }
}
