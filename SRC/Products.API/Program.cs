using Microsoft.EntityFrameworkCore;
using Products.API.Database;
using Products.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(opt => 
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions => 
    {
        // 🛡️ МАГИЯ: Автоматические повторные попытки при временных сбоях сети/старта БД
        npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,                // Максимум 5 попыток
        maxRetryDelay: TimeSpan.FromSeconds(10), // Пауза между попытками
        errorCodesToAdd: null);          // null = использовать стандартные коды ошибок PostgreSQL

    });

    // Включаем логирование значений параметров (только для Development!)
    if (builder.Environment.IsDevelopment())
    {
        opt.EnableSensitiveDataLogging();
        opt.EnableDetailedErrors();        // Бонусом — больше деталей об ошибках
    }
});
    

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
