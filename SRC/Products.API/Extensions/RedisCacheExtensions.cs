using Serilog;
using StackExchange.Redis;

namespace Products.API.Extensions;

public static class RedisCacheExtensions
{
    public static async Task InvalidateProductsListCacheAsync(this IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());

        // Паттерн должен совпадать с тем, что мы используем в Program.cs + InstanceName
        const string pattern = "ProductsAPI_products_list_*";

        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            var deletedCount = await db.KeyDeleteAsync(keys);
            // Т.к. подключили Serilog, переходим на логирование:
            // Console.WriteLine($"🧹 CACHE INVALIDATED! Удалено ключей кэша: {deletedCount}");
            Log.Information("🧹 CACHE INVALIDATED! Удалено ключей кэша: {DeletedCount}", deletedCount);
        }
        else
        {
            //Console.WriteLine("🧹 Кэш списка был чист, удалять нечего.");
            Log.Information("🧹 Кэш списка был чист, удалять нечего.");
        }
    }
}
