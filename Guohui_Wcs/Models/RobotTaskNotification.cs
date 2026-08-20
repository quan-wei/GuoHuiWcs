using System.Text.Json;

using System.Text.Json.Serialization;

namespace Guohui_Wcs.Models
{
    public class RobotTaskNotification
    {
        [JsonPropertyName("reqCode")]
        public string? ReqCode { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("taskCode")]
        public string? TaskCode { get; set; }

        [JsonPropertyName("wbCode")]
        public string? WbCode { get; set; }

        [JsonPropertyName("podCode")]
        public string? PodCode { get; set; }
    }
}
