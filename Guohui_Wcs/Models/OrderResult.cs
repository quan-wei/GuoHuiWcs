using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guohui_Wcs.Entity
{
    public class OrderResult
    {
        public string? code { get; set; }
        public string? message { get; set; }
        public string? data { get; set; }
        public bool @interrupt { get; set; }
        public string? reqCode { get; set; }
    }
}
