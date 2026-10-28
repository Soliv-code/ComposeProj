using Serilog;
using StackExchange.Redis;

namespace Products.API.Extensions;

public static class RedisCacheExtensions
{
    // ========================================================
    // 1. РЕГИСТРАЦИЯ В DI (Вызывается один раз при старте)
    // ========================================================
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "redis:6379";
        var redisInstanceName = configuration["Redis:InstanceName"] ?? "ProductsAPI_";

        // Высокоуровневая абстракция для кэширования (GetAsync / SetAsync)
        services.AddStackExchangeRedisCache(opt =>
        {
            opt.Configuration = redisConnectionString;
            opt.InstanceName = redisInstanceName;
        });

        // Низкоуровневый доступ для продвинутых операций (инвалидация по паттерну, SCAN, DEL)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        return services;
    }

    // ========================================================
    // 2. МЕТОДЫ ДЛЯ РАБОТЫ С КЭШЕМ (Вызываются в рантайме)
    // ========================================================
    public static async Task InvalidateProductsListCacheAsync(this IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());

        // Паттерн должен совпадать с тем, что мы используем при кешировании + InstanceName
        // Например, если ключ был "ProductsAPI_products_list_page_1", паттерн "ProductsAPI_products_list_*" его найдёт
        const string pattern = "ProductsAPI_products_list_*";

        // Внимание: server.Keys может быть медленным на огромных базах, но для нашего API это ок.
        // В продакшене с миллионами ключей лучше использовать SCAN через Lua-скрипт.
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            var deletedCount = await db.KeyDeleteAsync(keys);
            Log.Information("🧹 CACHE INVALIDATED! Удалено ключей кэша: {DeletedCount}", deletedCount);
        }
        else
        {
            Log.Information("🧹 Кэш списка был чист, удалять нечего.");
        }
    }
}