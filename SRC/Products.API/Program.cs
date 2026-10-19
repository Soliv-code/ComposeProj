using Microsoft.EntityFrameworkCore;
using Products.API.Database;
using Products.API.Endpoints;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(opt => 
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions => 
    {
        // 🛡️ МАГИЯ: Автоматические повторные попытки при временных сбоях сети/старта БД
        npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,                           // Максимум 5 попыток
        maxRetryDelay: TimeSpan.FromSeconds(10),    // Пауза между попытками
        errorCodesToAdd: null);                     // null = использовать стандартные коды ошибок PostgreSQL

    });

    // Включаем логирование значений параметров (только для Development!)
    if (builder.Environment.IsDevelopment())
    {
        opt.EnableSensitiveDataLogging();
        opt.EnableDetailedErrors();        // Бонусом — больше деталей об ошибках
    }
});

// Читаем настройки Redis из конфигурации (appsettings.json или переменных окружения)
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "redis:6379";
var redisInstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "ProductsAPI_";

// Подключаем Redis как распределенный кэш
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = redisConnectionString;
    opt.InstanceName = redisInstanceName;
});

// Регистрируем IConnectionMultiplexer для полного контроля (инвалидация кэша)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));

/*
// Подключаем Redis как распределенный кэш
builder.Services.AddStackExchangeRedisCache(opt =>
{
    // "redis" - это имя сервиса из docker-compose.yml, 6379 - стандартный порт
    opt.Configuration = "redis:6379";
    // Префикс для всех ключей кэша, чтобы они не путались с другими приложениями, если они будут
    opt.InstanceName = "ProductsAPI_";
});

// Регистрируем IConnectionMultiplexer, чтобы иметь полный контроль над Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("redis:6379"));
*/
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
