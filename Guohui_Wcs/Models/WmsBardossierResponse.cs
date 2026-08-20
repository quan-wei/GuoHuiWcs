using Newtonsoft.Json;

namespace Guohui_Wcs.Models;

public class WmsBardossierResponse
{
    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("fixed_filter")]
    public string FixedFilter { get; set; } = string.Empty;

    [JsonProperty("sub_title")]
    public string SubTitle { get; set; } = string.Empty;

    [JsonProperty("page_count")]
    public int PageCount { get; set; }

    [JsonProperty("params_for_create")]
    public string ParamsForCreate { get; set; } = string.Empty;

    [JsonProperty("info_list")]
    public List<BardossierItem> InfoList { get; set; } = new();
}

public class BardossierItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>条码号</summary>
    [JsonProperty("number")]
    public string Number { get; set; } = string.Empty;

    /// <summary>物料编码</summary>
    [JsonProperty("material$number")]
    public string MaterialNumber { get; set; } = string.Empty;

    /// <summary>规格型号</summary>
    [JsonProperty("material$model")]
    public string MaterialModel { get; set; } = string.Empty;

    /// <summary>数量</summary>
    [JsonProperty("qty")]
    public string Qty { get; set; } = string.Empty;

    /// <summary>辅助数量</summary>
    [JsonProperty("auxqty")]
    public string AuxQty { get; set; } = string.Empty;

    /// <summary>来源单号</summary>
    [JsonProperty("billnum")]
    public string BillNum { get; set; } = string.Empty;

    /// <summary>批次</summary>
    [JsonProperty("pc")]
    public string Pc { get; set; } = string.Empty;

    /// <summary>行号</summary>
    [JsonProperty("lineno")]
    public string Lineno { get; set; } = string.Empty;

    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("ischanged")]
    public bool IsChanged { get; set; }

    [JsonProperty("ismix")]
    public bool IsMix { get; set; }

    [JsonProperty("level")]
    public int Level { get; set; }

    [JsonProperty("material")]
    public NameTitle? Material { get; set; }

    [JsonProperty("warehouse")]
    public NameTitle? Warehouse { get; set; }

    [JsonProperty("location")]
    public NameTitle? Location { get; set; }

    [JsonProperty("unit")]
    public NameTitle? Unit { get; set; }

    [JsonProperty("customer")]
    public NameTitle? Customer { get; set; }

    [JsonProperty("billtype")]
    public NameTitle? BillType { get; set; }

    [JsonProperty("barrule")]
    public NameTitle? BarRule { get; set; }

    [JsonProperty("barstatus")]
    public NameTitle? BarStatus { get; set; }

    [JsonProperty("bartype")]
    public BartypeInfo? BarType { get; set; }

    [JsonProperty("checkstatus")]
    public CheckStatusInfo? CheckStatus { get; set; }
}

public class NameTitle
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("_type")]
    public string? Type { get; set; }

    [JsonProperty("number")]
    public string? Number { get; set; }
}

public class BartypeInfo
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}

public class CheckStatusInfo
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}
