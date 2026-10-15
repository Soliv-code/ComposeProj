using Microsoft.EntityFrameworkCore;
using Products.API.Database;

namespace Products.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (ApplicationDbContext context, CancellationToken ct) =>
        {
            var products = await context.Products
                .AsNoTracking()
                .ToListAsync(ct);
            return Results.Ok(products);
        });
        /*
        app.MapGet("/products", async (CancellationToken ct) =>
        {
            return Results.Ok("test");
        });
        */
    }
}
