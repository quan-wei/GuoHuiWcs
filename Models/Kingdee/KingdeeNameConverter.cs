using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Guohui_Wcs.Models.Kingdee;

/// <summary>
/// 金蝶 Name 字段转换器：从 [{"Key":2052,"Value":"中文名"}] 中提取中文名
/// </summary>
public class KingdeeNameConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(string);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        if (reader.TokenType == JsonToken.String)
            return reader.Value?.ToString();

        var arr = JArray.Load(reader);
        foreach (var item in arr)
        {
            if (item is JObject obj && obj["Key"]?.Value<int>() == 2052)
                return obj["Value"]?.Value<string>();
        }
        return arr.First is JObject first ? first["Value"]?.Value<string>() : null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }
}
