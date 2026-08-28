using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Services;
using NLog;
using NLog.Web;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 业务服务注册
builder.Services.AddSingleton(_ => Model_Data.Db);
builder.Services.AddHttpClient<ApiClientService>();
builder.Services.AddScoped<LocationAllocationService>();
builder.Services.AddSingleton<KingdeeApiService>();
builder.Services.AddScoped<DeliveryOrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var requestLogger = LogManager.GetLogger("RequestLogger");
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;

    requestLogger.Info("收到请求报文 Method={Method} Path={Path} Body={Body}", context.Request.Method, context.Request.Path, body);
    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
