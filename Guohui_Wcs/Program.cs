using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 业务服务注册
builder.Services.AddSingleton(_ => Model_Data.Db);
builder.Services.AddHttpClient<ApiClientService>();
builder.Services.AddScoped<LocationAllocationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();