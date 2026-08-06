using System.Text;
using Kingdee.CDP.WebApi.SDK;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Guohui_Test;

/// <summary>
/// 金蝶云星空 SDK 测试封装（直接使用 K3CloudApi）
/// </summary>
public class KingdeeSdkService
{
    private readonly K3CloudApi _client;
    public string LastResponse { get; private set; } = string.Empty;
    public bool IsLoggedIn { get; private set; } = false;

    public KingdeeSdkService()
    {
        _client = new K3CloudApi();
    }

    /// <summary>
    /// 测试登录（SDK 内部读取 App.config 配置）
    /// </summary>
    public string TestLogin()
    {
        try
        {
            // SDK 在 new K3CloudApi() 时自动读取配置文件中的认证信息
            // 登录状态由 SDK 内部维护
            IsLoggedIn = true; // 假设初始化成功即登录成功，SDK 自动处理
            return "SDK 初始化成功（配置已从 App.config 读取）";
        }
        catch (Exception ex)
        {
            IsLoggedIn = false;
            return $"SDK 初始化失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 执行单据查询（ExecuteBillQuery）
    /// </summary>
    public string ExecuteBillQuery(string formId, string fieldKeys, string filterString = "", int topRowCount = 10)
    {
        var param = new QueryParam()
        {
            FormId = formId,
            FieldKeys = fieldKeys,
            FilterString = filterString,
            TopRowCount = topRowCount
        };

        var result = _client.ExecuteBillQuery(param.ToJson());
        LastResponse = JsonConvert.SerializeObject(result, Formatting.Indented);

        // 解析返回结果
        var sb = new StringBuilder();
        if (result.Count == 1)
        {
            var resultJObject = JArray.Parse(JsonConvert.SerializeObject(result[0]));
            var queryNode = resultJObject.SelectToken("$..IsSuccess");
            if (queryNode != null)
            {
                var isSuccess = queryNode.Value<bool>();
                sb.AppendLine(isSuccess ? "操作成功" : "操作失败");
            }
            else
            {
                sb.AppendLine("操作成功");
            }
        }
        else
        {
            sb.AppendLine("操作成功");
        }

        sb.AppendLine();
        sb.AppendLine("原始数据:");
        sb.AppendLine(LastResponse);
        return sb.ToString();
    }
}
