using Guohui_Wcs.Models.Kingdee;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Guohui_Wcs.Services;

public class KingdeeApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private readonly ILogger<KingdeeApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _acctId;
    private readonly string _username;
    private readonly string _password;
    private readonly string _lcid;

    private bool _isLoggedIn;
    private string? _cookieString;
    private static readonly SemaphoreSlim _loginLock = new(1, 1);

    public KingdeeApiService(IConfiguration configuration, ILogger<KingdeeApiService> logger)
    {
        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
        _logger = logger;

        _baseUrl = configuration.GetValue<string>("Kingdee:BaseUrl") ?? "http://191.167.10.102:2126/K3Cloud/";
        _acctId = configuration.GetValue<string>("Kingdee:AcctId") ?? "";
        _username = configuration.GetValue<string>("Kingdee:UserName") ?? "";
        _password = configuration.GetValue<string>("Kingdee:Password") ?? "";
        _lcid = (configuration.GetValue<string>("Kingdee:Lcid") ?? "2052").Trim();
    }

    public void Dispose() { _httpClient.Dispose(); }

    public async Task<bool> EnsureLoginAsync()
    {
        if (_isLoggedIn) return true;
        await _loginLock.WaitAsync();
        try
        {
            if (_isLoggedIn) return true;
            _isLoggedIn = await LoginInternalAsync(_acctId, _username, _password, _lcid);
            return _isLoggedIn;
        }
        finally { _loginLock.Release(); }
    }

    private async Task<bool> LoginInternalAsync(string acctId, string username, string password, string lcid)
    {
        try
        {
            var url = $"{_baseUrl}Kingdee.BOS.WebApi.ServicesStub.AuthService.ValidateUser.common.kdsvc";
            var json = JsonConvert.SerializeObject(new { acctID = acctId, username, password, lcid });
            _logger.LogInformation("Kingdee login body: {Body}", json);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0");
            request.Content = new StringContent(json) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") } };

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var result = JObject.Parse(responseBody);
            var ok = result["LoginResultType"]?.Value<int>() == 1;
            if (ok)
            {
                var baseUri = new Uri(_baseUrl);
                var cookieList = new List<string>();
                foreach (Cookie c in _cookieContainer.GetCookies(baseUri))
                    cookieList.Add($"{c.Name}={c.Value}");
                cookieList.Add("l=cn");
                _cookieString = string.Join("; ", cookieList);
                _logger.LogInformation("Kingdee login OK, cookies: {Cookies}", _cookieString);
            }
            return ok;
        }
        catch (Exception ex) { _logger.LogError(ex, "Kingdee login exception"); return false; }
    }

    private async Task<string?> PostAsync(string endpoint, object body)
    {
        if (!await EnsureLoginAsync()) return null;
        try
        {
            var url = $"{_baseUrl}{endpoint}";
            var json = JsonConvert.SerializeObject(body);
            _logger.LogInformation("Kingdee POST {Url} body: {Body}", endpoint, json);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0");
            request.Content = new StringContent(json) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") } };
            if (!string.IsNullOrEmpty(_cookieString)) request.Headers.Add("Cookie", _cookieString);

            using var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Kingdee POST {Url} response status={Status}, body={Body}", endpoint, (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
            return responseBody;
        }
        catch (Exception ex) { _logger.LogError(ex, "Kingdee POST {Url} failed", endpoint); return null; }
    }

    /// <summary>
    /// 通过 ExecuteBillQuery 接口查询单据（不依赖 View 接口的网页 Cookie）
    /// </summary>
    public async Task<JArray?> ExecuteBillQueryAsync(string formId, string fieldKeys, string filterString = "", int topRowCount = 100)
    {
        var json = await PostAsync("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc",
            new { formId, fieldKeys, filterString, orderString = "", topRowCount, startRow = 0, limit = 0 });
        if (json == null) return null;
        try { return JArray.Parse(json); }
        catch { return null; }
    }

    /// <summary>
    /// 通过 View 接口查询单据（需要网页登录 Cookie，API 登录可能不适用）
    /// </summary>
    public async Task<KingdeeViewResponse<T>?> ViewAsync<T>(string formId, string number, string id = "")
    {
        var dataJson = JsonConvert.SerializeObject(new { Number = number, Id = id });
        var json = await PostAsync("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.View.common.kdsvc",
            new { formid = formId, data = dataJson });
        if (json == null) return null;
        try { return JsonConvert.DeserializeObject<KingdeeViewResponse<T>>(json); }
        catch (JsonException ex) { _logger.LogError(ex, "Deserialize failed"); return null; }
    }
}
