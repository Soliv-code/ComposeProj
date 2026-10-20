using Microsoft.EntityFrameworkCore;
using Products.API.Database;
using Products.API.Endpoints;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
//======================================= Serilog block =======================================
// Настраиваем Serilog: пишем и в консоль (для Docker), и в Seq
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://seq:5341")
    .MinimumLevel.Information());
//=============================================================================================
builder.Services.AddDbContext<ApplicationDbContext>(opt => 
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions => 
    {
        // Автоматические повторные попытки при временных сбоях сети/старта БД
        npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,                           // Максимум 5 попыток
        maxRetryDelay: TimeSpan.FromSeconds(10),    // Пауза между попытками
        errorCodesToAdd: null);                     // null = использовать стандартные коды ошибок PostgreSQL

    });

    // Включаем логирование значений параметров (только для Development!)
    if (builder.Environment.IsDevelopment())
    {
        opt.EnableSensitiveDataLogging();  // Значение параметров для команд отправляемых в БД 
        opt.EnableDetailedErrors();        // Больше деталей об ошибках
    }
});
//================================================== Redis block ===============================================================
// Читаем настройки Redis из конфигурации (appsettings.json или переменных окружения)
// Может возникнуть вопрос - а где указатель читать из .env ?
// Магия .NET Configuration System:
// .NET ищет это значение сразу в нескольких местах, в порядке возрастания приоритета:
// 1. appsettings.json (низший приоритет)
// 2. appsettings.Development.json
// 3. Переменные окружения (Environment Variables) (высший приоритет для наших целей)
// 4. Аргументы командной строки
/*
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "redis:6379";
var redisInstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "ProductsAPI_";
*/
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
var redisInstanceName = builder.Configuration["Redis:InstanceName"] ?? "ProductsAPI_";
// Подключаем Redis как распределенный кэш
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = redisConnectionString;
    opt.InstanceName = redisInstanceName;
});

// Регистрируем IConnectionMultiplexer для полного контроля (инвалидация кэша)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
//==============================================================================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();     // отдаёт /swagger/v1/swagger.json
    app.UseSwaggerUI();
}
app.MapProductEndpoints();
// app.UseHttpsRedirection(); Для ебаного docker-compose!
 
app.Run();
