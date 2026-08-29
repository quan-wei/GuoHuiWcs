using Guohui_Wcs.Models;
using GuoHui_Data.DaoEntity;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using NLog;

namespace Guohui_Wcs.Services;

public class LoginResult
{
    public bool Success { get; set; }
    public string? Session { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ApiClientService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClientService> _logger;
    private readonly bool _ownsHttpClient;
    private readonly string _baseUrl;
    private static readonly Logger RequestLogger = LogManager.GetLogger("RequestLogger");

    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(2);

    private static string? _cachedSession;
    private static string? _cachedLocale;
    private static DateTime _lastLoginTime = DateTime.MinValue;
    private static readonly SemaphoreSlim _sessionLock = new(1, 1);

    public ApiClientService(HttpClient httpClient, ILogger<ApiClientService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ownsHttpClient = false;
        _baseUrl = configuration.GetValue<string>("Wms:BaseUrl")
            ?? throw new InvalidOperationException("缺少配置 Wms:BaseUrl");
    }

    public ApiClientService(ILogger<ApiClientService> logger, IConfiguration configuration)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
        _logger = logger;
        _ownsHttpClient = true;
        _baseUrl = configuration.GetValue<string>("Wms:BaseUrl")
            ?? throw new InvalidOperationException("缺少配置 Wms:BaseUrl");
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    public async Task<string> GetSessionAsync()
    {
        if (_cachedSession != null && (DateTime.Now - _lastLoginTime) < SessionLifetime)
            return _cachedSession;

        await _sessionLock.WaitAsync();
        try
        {
            if (_cachedSession != null && (DateTime.Now - _lastLoginTime) < SessionLifetime)
                return _cachedSession;

            return await LoginAndCacheSessionAsync();
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private async Task<string> LoginAndCacheSessionAsync()
    {
        var result = await LoginAsync();
        if (!result.Success)
            throw new InvalidOperationException($"Login failed: {result.ErrorMessage}");

        _cachedSession = result.Session;
        _cachedLocale = "cn";
        _lastLoginTime = DateTime.Now;
        return _cachedSession!;
    }

    /// <summary>
    /// 服务端会话失效后重新登录。若其他线程已经用新会话刷新过缓存，则直接复用，避免重复登录。
    /// </summary>
    private async Task<string> RefreshSessionAsync(string failedSession)
    {
        await _sessionLock.WaitAsync();
        try
        {
            if (_cachedSession != null && _cachedSession != failedSession
                && (DateTime.Now - _lastLoginTime) < SessionLifetime)
                return _cachedSession;

            return await LoginAndCacheSessionAsync();
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private static bool IsSessionTimeoutResponse(string responseBody)
    {
        return responseBody != null
            && (responseBody.Contains("server.error.session_timeout")
                || responseBody.Contains("UserSessionTimeoutException"));
    }

    private static string ExtractSessionErrorMessage(string responseBody)
    {
        try
        {
            var json = JObject.Parse(responseBody);
            return json["#message"]?.Value<string>() ?? responseBody;
        }
        catch
        {
            return responseBody;
        }
    }

    private async Task<(int StatusCode, string Body)> SendInvokeAsync(string session, string endpoint, string jsonBody)
    {
        var url = $"{_baseUrl}/web/api/invoke/{session}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Cookie", $"a={session};l={_cachedLocale};r=f8a4125c41d4407986c0fb545090c3b5");
        request.Content = new StringContent(jsonBody)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain") }
        };

        RequestLogger.Info("出站请求 Endpoint={Endpoint} Body={Body}", endpoint, jsonBody);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        RequestLogger.Info("出站响应 Endpoint={Endpoint} Status={Status} Body={Body}", endpoint, (int)response.StatusCode, responseBody);

        return ((int)response.StatusCode, responseBody);
    }

    public async Task<string> InvokeAsync(string endpoint, string jsonBody)
    {
        var session = await GetSessionAsync();
        var (statusCode, responseBody) = await SendInvokeAsync(session, endpoint, jsonBody);

        if (IsSessionTimeoutResponse(responseBody))
        {
            _logger.LogWarning("WMS session expired, re-login and retry. Endpoint={Endpoint}", endpoint);
            session = await RefreshSessionAsync(session);
            (statusCode, responseBody) = await SendInvokeAsync(session, endpoint, jsonBody);

            if (IsSessionTimeoutResponse(responseBody))
                throw new InvalidOperationException(
                    $"WMS session still invalid after re-login: {ExtractSessionErrorMessage(responseBody)}");
        }

        if (statusCode < 200 || statusCode >= 300)
            throw new HttpRequestException($"WMS invoke failed, endpoint={endpoint}, status={statusCode}");

        return responseBody;
    }

    public async Task<WmsBardossierResponse> SelectBardossierAsync(string barcode)
    {
        var json = await InvokeAsync("wms_bardossier.select",
            $"{{\"number\": \"{barcode}\"}}");
        return JsonConvert.DeserializeObject<WmsBardossierResponse>(json)
               ?? new WmsBardossierResponse();
    }

    public async Task<Barcode?> SyncBardossierToDbAsync(string barcode)
    {
        var result = await SelectBardossierAsync(barcode);
        if (result.Total == 0 || result.InfoList.Count == 0)
        {
            _logger.LogWarning("WMS returned no data for barcode: {Barcode}", barcode);
            return null;
        }

        var item = result.InfoList[0];
        var entity = MapToBarcode(item);
        var db = Model_Data.Db;

        var existing = await db.Queryable<Barcode>()
            .FirstAsync(b => b.Number == barcode);

        if (existing != null)
        {
            entity.Id = existing.Id;
            entity.CreateTime = existing.CreateTime;
            entity.UpdateTime = DateTime.Now;
            await db.Updateable(entity).ExecuteCommandAsync();
            _logger.LogInformation("Updated barcode: {Barcode}", barcode);
        }
        else
        {
            entity.CreateTime = DateTime.Now;
            entity.Id = await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
            _logger.LogInformation("Inserted barcode: {Barcode}", barcode);
        }

        return entity;
    }

    private static Barcode MapToBarcode(BardossierItem item)
    {
        return new Barcode
        {
            Number = item.Number,
            BarType = item.BarType?.Value ?? "",
            BarStatus = MapBarStatus(item.BarStatus?.Name),
            Qty = decimal.TryParse(item.Qty, out var q) ? q : 0,
            AuxQty = decimal.TryParse(item.AuxQty, out var aq) ? aq : null,
            CheckStatus = item.CheckStatus?.Value ?? "",
            WarehouseId = item.Warehouse?.Id,
            WarehouseName = item.Warehouse?.Name,
            LocationId = item.Location?.Id,
            LocationName = item.Location?.Name,
            MaterialId = item.Material?.Id,
            MaterialNo = item.MaterialNumber,
            MaterialName = item.Material?.Name,
            MaterialModel = item.MaterialModel,
            PC = item.Pc,
            CustomerId = item.Customer?.Id,
            CustomerName = item.Customer?.Name
        };
    }

    private static byte MapBarStatus(string? name)
    {
        return name switch
        {
            "可用" => 1,
            _ => 0
        };
    }

    private async Task<LoginResult> LoginAsync(string username = "dev", string password = "1234")
    {
        try
        {
            var url = $"{_baseUrl}/web/api/login?u={Uri.EscapeDataString(username)}&p={Uri.EscapeDataString(password)}";
            _logger.LogInformation("Login request: {Url}", $"{_baseUrl}/web/api/login?u={Uri.EscapeDataString(username)}&p=***");

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Login response: {Content}", content);

            var json = JObject.Parse(content);

            var session = json["a"]?.Value<string>();
            if (string.IsNullOrEmpty(session))
                return new LoginResult { Success = false, ErrorMessage = $"No 'a' field in response: {content}" };

            _logger.LogInformation("Login OK, user: {UserName}", json["user_name"]);

            return new LoginResult
            {
                Success = true,
                Session = session,
                UserId = json["user_id"]?.Value<string>(),
                UserName = json["user_name"]?.Value<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return new LoginResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
