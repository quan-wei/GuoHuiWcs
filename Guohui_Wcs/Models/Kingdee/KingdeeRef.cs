using Newtonsoft.Json;

namespace Guohui_Wcs.Models.Kingdee;

/// <summary>
/// 金蝶基础资料引用（物料、客户、仓库、单位等通用）
/// </summary>
public class KingdeeRef
{
    [JsonProperty("Id")]
    public string? Id { get; set; }

    [JsonProperty("Number")]
    public string? Number { get; set; }

    [JsonProperty("Name")]
    [JsonConverter(typeof(KingdeeNameConverter))]
    public string? Name { get; set; }
}
