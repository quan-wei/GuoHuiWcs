using Newtonsoft.Json;

namespace Guohui_Wcs.Models;

public class WmsBardossierResponse
{
    public int total { get; set; }
    public string fixed_filter { get; set; } = string.Empty;
    public string sub_title { get; set; } = string.Empty;
    public int page_count { get; set; }
    public string params_for_create { get; set; } = string.Empty;
    public List<BardossierItem> info_list { get; set; } = new();
}

public class BardossierItem
{
    public string id { get; set; } = string.Empty;

    /// <summary>条码号</summary>
    public string number { get; set; } = string.Empty;

    /// <summary>物料编码</summary>
    [JsonProperty("material$number")]
    public string material_number { get; set; } = string.Empty;

    /// <summary>规格型号</summary>
    [JsonProperty("material$model")]
    public string material_model { get; set; } = string.Empty;

    /// <summary>数量</summary>
    public string qty { get; set; } = string.Empty;

    /// <summary>辅助数量</summary>
    public string auxqty { get; set; } = string.Empty;

    /// <summary>来源单号</summary>
    public string billnum { get; set; } = string.Empty;

    /// <summary>批次</summary>
    public string pc { get; set; } = string.Empty;

    /// <summary>行号</summary>
    public string lineno { get; set; } = string.Empty;

    public int version { get; set; }
    public bool ischanged { get; set; }
    public bool ismix { get; set; }
    public int level { get; set; }

    public NameTitle? material { get; set; }
    public NameTitle? warehouse { get; set; }
    public NameTitle? location { get; set; }
    public NameTitle? unit { get; set; }
    public NameTitle? customer { get; set; }
    public NameTitle? billtype { get; set; }
    public NameTitle? barrule { get; set; }
    public NameTitle? barstatus { get; set; }
    public BartypeInfo? bartype { get; set; }
    public CheckStatusInfo? checkstatus { get; set; }
}

public class NameTitle
{
    public string name { get; set; } = string.Empty;
    public string id { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string? _type { get; set; }
    public string? number { get; set; }
}

public class BartypeInfo
{
    public string title { get; set; } = string.Empty;
    public string value { get; set; } = string.Empty;
}

public class CheckStatusInfo
{
    public string title { get; set; } = string.Empty;
    public string value { get; set; } = string.Empty;
}
