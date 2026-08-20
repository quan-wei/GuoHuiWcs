using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Guohui_Wcs.Entity
{
    public class OrderResult
    {
        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("data")]
        public string? Data { get; set; }

        [JsonProperty("interrupt")]
        public bool Interrupt { get; set; }

        [JsonProperty("reqCode")]
        public string? ReqCode { get; set; }
    }
}
