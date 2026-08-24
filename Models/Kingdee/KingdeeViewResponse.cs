using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Guohui_Wcs.Models.Kingdee;

/// <summary>
/// 金蝶 View 接口通用外层响应
/// </summary>
public class KingdeeViewResponse<T>
{
    [JsonProperty("Result")]
    public KingdeeViewResult<T> Result { get; set; } = new();
}

public class KingdeeViewResult<T>
{
    [JsonProperty("ResponseStatus")]
    public KingdeeResponseStatus ResponseStatus { get; set; } = new();

    [JsonProperty("Result")]
    public T? Data { get; set; }
}

public class KingdeeResponseStatus
{
    [JsonProperty("IsSuccess")]
    public bool IsSuccess { get; set; }
}
