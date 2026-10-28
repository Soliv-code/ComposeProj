using Microsoft.OpenApi;
using Products.API.Endpoints;
using Products.API.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ========================================================
// 1. ЛОГИРОВАНИЕ
// ========================================================
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://seq:5341")
    .MinimumLevel.Information());

// ========================================================
// 2. РЕГИСТРАЦИЯ ЗАВИСИМОСТЕЙ (DI)
// ========================================================
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// ========================================================
// 3. SWAGGER (ПРОСТОЙ И РАБОЧИЙ)
// ========================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // 1. Определяем схему "Bearer" (тут всё осталось как было)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите 'Bearer ' [пробел] и затем ваш JWT токен."
    });

    // 2. НОВЫЙ СИНТАКСИС ДЛЯ .NET 10 / SWASHBUCKLE 10.x
    // Метод теперь принимает делегат (document => ...), а для ссылки используем OpenApiSecuritySchemeReference [[13]], [[15]]
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// ========================================================
// 4. HTTP-PIPELINE
// ========================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapProductEndpoints();

// ========================================================
// 5. DEV-ИНСТРУМЕНТЫ (Только для локальной разработки!)
// ========================================================
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/get-token", async (IConfiguration config) =>
    {
        var clientSecret = config["Keycloak:ClientSecret"];
        if (string.IsNullOrEmpty(clientSecret))
        {
            return Results.Problem("ClientSecret не настроен. Проверь .env (KEYCLOAK_CLIENT_SECRET)", statusCode: 500);
        }

        using var httpClient = new HttpClient();

        // 👇 ИСПРАВЛЕНИЕ: Используем внутренний Docker-адрес для запроса, а не localhost!
        var internalUrl = config["Keycloak:InternalUrl"] ?? "http://keycloak:8080/realms/products-realm";
        var tokenUrl = $"{internalUrl}/protocol/openid-connect/token";

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", "products-api"),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("username", "testuser"),
            new KeyValuePair<string, string>("password", "testpassword")
        });

        var response = await httpClient.PostAsync(tokenUrl, requestContent);

        // Если Keycloak вернул ошибку (например, неверный пароль), пробросим её текст, а не 500
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            return Results.Content(errorText, "application/json", statusCode: (int)response.StatusCode);
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return Results.Content(jsonResponse, "application/json");
    })
    .WithTags("🛠 Dev Tools")
    .WithSummary("Получить JWT токен для тестов")
    .WithDescription("Обращается к Keycloak и возвращает access_token. Скопируйте значение 'access_token' из ответа и используйте его в кнопке 'Authorize' сверху. Работает только в режиме Development!");
}

app.Run();