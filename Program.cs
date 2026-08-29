using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Services;
using Guohui_Wcs.Utils.AGVUtils;
using Microsoft.AspNetCore.Diagnostics;
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
builder.Services.AddSingleton(_ => new AGVOrderHelper(
    builder.Configuration.GetValue<string>("Agv:BaseUrl")
    ?? throw new InvalidOperationException("缺少配置 Agv:BaseUrl")));

var app = builder.Build();

var requestLogger = LogManager.GetLogger("RequestLogger");

// 全局异常处理：未捕获异常统一记录日志并返回 { Success, Message } 结构
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        requestLogger.Error(feature?.Error, "未处理异常 {Path}", context.Request.Path);
        var jsonOptions = context.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Mvc.JsonOptions>()
            .JsonSerializerOptions;
        await context.Response.WriteAsJsonAsync(
            new { Success = false, Message = "服务器内部错误，请查看服务端日志" }, jsonOptions);
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
