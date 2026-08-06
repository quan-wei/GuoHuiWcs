using Guohui_Wcs.Models;
using GuoHui_Data.DaoEntity;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

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

    private const string BaseUrl = "http://191.167.10.102:8081";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);

    private static string? _cachedSession;
    private static string? _cachedLocale;
    private static DateTime _lastLoginTime = DateTime.MinValue;
    private static readonly SemaphoreSlim _sessionLock = new(1, 1);

    public ApiClientService(HttpClient httpClient, ILogger<ApiClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ownsHttpClient = false;
    }

    public ApiClientService(ILogger<ApiClientService> logger)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
        _logger = logger;
        _ownsHttpClient = true;
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

            var result = await LoginAsync();
            if (!result.Success)
                throw new InvalidOperationException($"Login failed: {result.ErrorMessage}");

            _cachedSession = result.Session;
            _cachedLocale = "cn";
            _lastLoginTime = DateTime.Now;
            return _cachedSession!;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    public async Task<string> InvokeAsync(string endpoint, string jsonBody)
    {
        var session = await GetSessionAsync();
        var url = $"{BaseUrl}/web/api/invoke/{session}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Cookie", $"a={session};l={_cachedLocale};r=f8a4125c41d4407986c0fb545090c3b5");
        request.Content = new StringContent(jsonBody)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain") }
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
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
        if (result.total == 0 || result.info_list.Count == 0)
        {
            _logger.LogWarning("WMS returned no data for barcode: {Barcode}", barcode);
            return null;
        }

        var item = result.info_list[0];
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
            Number = item.number,
            BarType = item.bartype?.value ?? "",
            BarStatus = MapBarStatus(item.barstatus?.name),
            Qty = decimal.TryParse(item.qty, out var q) ? q : 0,
            AuxQty = decimal.TryParse(item.auxqty, out var aq) ? aq : null,
            CheckStatus = item.checkstatus?.value ?? "",
            WarehouseId = item.warehouse?.id,
            WarehouseName = item.warehouse?.name,
            LocationId = item.location?.id,
            LocationName = item.location?.name,
            MaterialId = item.material?.id,
            MaterialNo = item.material_number,
            MaterialName = item.material?.name,
            MaterialModel = item.material_model,
            PC = item.pc,
            CustomerId = item.customer?.id,
            CustomerName = item.customer?.name
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
            var url = $"{BaseUrl}/web/api/login?u={Uri.EscapeDataString(username)}&p={Uri.EscapeDataString(password)}";
            _logger.LogInformation("Login request: {Url}", url);

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