using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaConnection : VdaHeader
    {
        [JsonPropertyName("connectionState")] 
        public string ConnectionState { get; set; } = "ONLINE"; 
    }
}
