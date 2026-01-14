using System.Text.Json.Serialization;

namespace VehicleSim.Core.VdaModels
{
    public class VdaError
    {
        [JsonPropertyName("errorType")]
        public required string ErrorType { get; set; }

        [JsonPropertyName("errorDescription")]
        public required string ErrorDescription { get; set; }

        [JsonPropertyName("errorLevel")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required VdaErrorLevel ErrorLevel { get; set; } 
    }
}
