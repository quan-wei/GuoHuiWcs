using Newtonsoft.Json;

namespace Guohui_Wcs.Models.Kingdee;

public class KingdeeDeliveryNotice
{
    [JsonProperty("Id")]
    public long Id { get; set; }

    [JsonProperty("BillNo")]
    public string? BillNo { get; set; }

    [JsonProperty("DocumentStatus")]
    public string? DocumentStatus { get; set; }

    [JsonProperty("Date")]
    public DateTime? Date { get; set; }

    [JsonProperty("Note")]
    public string? Note { get; set; }

    [JsonProperty("CustomerID")]
    public KingdeeRef? Customer { get; set; }

    [JsonProperty("SaleOrgId")]
    public KingdeeRef? SaleOrg { get; set; }

    [JsonProperty("StockOrgId")]
    public KingdeeRef? StockOrg { get; set; }

    [JsonProperty("DeliveryDeptID")]
    public KingdeeRef? DeliveryDept { get; set; }

    [JsonProperty("SAL_DELIVERYNOTICEENTRY")]
    public List<KingdeeDeliveryNoticeEntry>? Entries { get; set; }
}

public class KingdeeDeliveryNoticeEntry
{
    [JsonProperty("Id")]
    public long Id { get; set; }

    [JsonProperty("Seq")]
    public int Seq { get; set; }

    [JsonProperty("MaterialID")]
    public KingdeeRef? Material { get; set; }

    [JsonProperty("Qty")]
    public decimal Qty { get; set; }

    [JsonProperty("UnitID")]
    public KingdeeRef? Unit { get; set; }

    [JsonProperty("StockID")]
    public KingdeeRef? Stock { get; set; }

    /// <summary>批号（Lot 字段可能是对象，用 Lot_Text 取文本值）</summary>
    [JsonProperty("Lot_Text")]
    public string? Lot { get; set; }

    [JsonProperty("StockStatusID")]
    public KingdeeRef? StockStatus { get; set; }

    /// <summary>备注（金蝶字段名为 NoteEntry）</summary>
    [JsonProperty("NoteEntry")]
    public string? Note { get; set; }
}
