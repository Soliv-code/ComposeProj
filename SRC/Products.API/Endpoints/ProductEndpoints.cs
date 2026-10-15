using Microsoft.EntityFrameworkCore;
using Products.API.Database;
using Products.API.Entities;
using Products.API.Entities.DTOs;
using System.Runtime.CompilerServices;

namespace Products.API.Endpoints;

public static class ProductEndpoints
{
    // Московский часовой пояс
    private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        /*
        app.MapGet("/products", async (ApplicationDbContext context, CancellationToken ct) =>
        {
            var products = await context.Products
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(products);
        });
        */

        // GET /products?page=1&pageSize=10 — пагинация
        app.MapGet("/products", async (
            ApplicationDbContext ctx, 
            CancellationToken ct, 
            int page = 1, 
            int pageSize = 10) =>
        {
            // Валидация входных параметров 
            if(page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100)  pageSize = 10;

            var totalCount = await ctx.Products.CountAsync(ct);

            var products = await ctx.Products
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt)     // Стабильная сортировка для пагинации
                .ThenBy(p => p.Id)             // На случай одинакового created_at
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Конвертируем в московское время
            var response = products.Select(p => new ProductResponseDto(
                p.Id,
                p.Name,
                p.Price,
                p.Description,
                ConvertToMoscowTime(p.CreatedAt),
                ConvertToMoscowTime(p.UpdatedAt)
            ));

            return Results.Ok(new
            {
                data = products,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });

        })
        // Новые стандарты .NET 10!
        .WithName("GetProductsPaginated")                                       // Имя операции (для генерации клиентов)
        .WithDescription("Возвращает список товаров с поддержкой пагинации")    // Описание для Swagger
        .WithTags("Products");                                                  // Группировка в Swagger UI
        
        // GET /products/{id} — один товар по UUID
        app.MapGet("/products/{id:guid}", async(
            ApplicationDbContext ctx,
            Guid id,
            CancellationToken ct) =>
        {
            var product = await ctx.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
                return Results.NotFound();

            // Конвертируем в московское время
            var response = new ProductResponseDto(
                product.Id,
                product.Name,
                product.Price,
                product.Description,
                ConvertToMoscowTime(product.CreatedAt),
                ConvertToMoscowTime(product.UpdatedAt)
            );

            return Results.Ok(response);
            //return product is not null ? Results.Ok(product) : Results.NotFound($"Не найден товар с Id: {id}");
        })
        // Новые стандарты .NET 10!
        .WithName("GetProductById")
        .WithDescription("Возвращает конкретный товар по его UUID")
        .WithTags("Products")
        .ProducesProblem(404);                     // Товар с таким ID не найден (404 Not Found)

        // POST /products
        app.MapPost("/products", async (
            ApplicationDbContext ctx,
            CreateProductDto dto,
            CancellationToken ct) =>
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync(ct);

            // Возвращаем с московским временем
            var response = new ProductResponseDto(
                product.Id,
                product.Name,
                product.Price,
                product.Description,
                ConvertToMoscowTime(product.CreatedAt),
                ConvertToMoscowTime(product.UpdatedAt)
            );

            return Results.Created($"/products/{product.Id}", response); // 201 + dto Response
            //return Results.Created($"/products/{product.Id}", product); // 201 + наш product!
        })
        .WithName("CreateProduct")
        .WithDescription("Создаёт новый товар")
        .WithTags("Products")
        .Produces<ProductResponseDto>(201)         // Явно указываем Swagger'у, что вернётся 201 и тип данных
        .ProducesValidationProblem(400);           // Указываем, что может быть ошибка валидации

        // PUT /products
        app.MapPut("/products", async (
            ApplicationDbContext ctx,
            Guid id,
            UpdateProductDto dto,
            CancellationToken ct) =>
        {
            // 1. Ищем существующий товар (FindAsync эффективнее, чем FirstOrDefault, для поиска по первичному ключу)
            // var product = await ctx.Products.FindAsync(new object[] { id }, ct);
            // Приводим к формату .net 10:
            var product = await ctx.Products.FindAsync([id], ct);

            if (product is null)
                return Results.NotFound(
                    new { message = $"Товар с ID {id} не найден!" });

            // 2. Обновляем поля
            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;

            // 3. Сохраняем изменения
            await ctx.SaveChangesAsync(ct);

            // 4. Возвращаем обновленный объект с московским временем

            var response = new ProductResponseDto(
                product.Id,
                product.Name,
                product.Price,
                product.Description,
                ConvertToMoscowTime(product.CreatedAt),
                ConvertToMoscowTime(product.UpdatedAt)
            );
            return Results.Ok(response);
        })
        .WithName("UpdateProduct")
        .WithDescription("Обновляет информацию о товаре по его ID")
        .WithTags("Products")
        .Produces<ProductResponseDto>(200)         // Успешное обновление (200 OK)
        .ProducesValidationProblem(400)            // Ошибка валидации DTO (400 Bad Request)
        .ProducesProblem(404);                     // Товар с таким ID не найден (404 Not Found)

        // DELETE /products/{id} — удаление товара
        app.MapDelete("/products/{id:guid}", async (
            ApplicationDbContext ctx,
            Guid id,
            CancellationToken ct
            ) =>
        {
            // 1. Ищем товар по первичному ключу
            var product = await ctx.Products.FindAsync([id], ct);

            if (product is null)
                return Results.NotFound(
                    new { message = $"Товар с ID {id} не найден" });

            // 2. Помечаем сущность на удаление
            ctx.Products.Remove(product);

            // 3. Сохраняем изменения в БД
            await ctx.SaveChangesAsync(ct);

            // 4. Возвращаем 204 No Content (стандарт для успешного DELETE)
            return Results.NoContent();

        })
        .WithName("DeleteProduct")
        .WithDescription("Удаляет товар по его уникальному идентификатору")
        .WithTags("Products")
        .Produces(204)               // Успешное удаление
        .ProducesProblem(404);       // Товар не найден
    }

    private static string ConvertToMoscowTime(DateTime utcTime)
    {
        var moscowTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, MoscowTimeZone);
        return moscowTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}