using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaPosition
    {
        [JsonPropertyName("positionInitialized")]
        public bool PositionInitialized = true;

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("theta")]
        public double Theta { get; set; }

        [JsonPropertyName("mapId")]
        public required string MapId { get; set; }
    }
}
