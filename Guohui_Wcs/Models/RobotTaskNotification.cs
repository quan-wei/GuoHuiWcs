using System.Text.Json;

namespace Guohui_Wcs.Models
{
    public class RobotTaskNotification
    {
        public string? reqCode { get; set; }

        public string? method { get; set; }
        public string? taskCode { get; set; }

        public string? wbCode { get; set; }
        public string? podCode { get; set; }

    }
}
