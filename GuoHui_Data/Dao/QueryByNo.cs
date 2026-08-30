using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Models
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("querybyno")]
    public partial class QueryByNo
    {
        public QueryByNo()
        {
        }

        public string? LocationCode { get; set; }
        public string? Reserve5 { get; set; }
        public string? LocationType { get; set; }
        public decimal? LimitWeightt { get; set; }
        public decimal? TotalWeight { get; set; }
        public string? PallNo { get; set; }
        public decimal? PallWeight { get; set; }
        public string? BarcodeNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? BarType { get; set; }
        public decimal? Qty { get; set; }
        public decimal? AuxQty { get; set; }
        public string? WarehouseName { get; set; }
        public string? MaterialNo { get; set; }
        public string? MaterialName { get; set; }
        public int? SubTitleIndex { get; set; }
        public string? SubTitleValue { get; set; }
        public decimal? CorrespondingWeight { get; set; }

    }
}
