using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Products.API.Database;
using Products.API.Entities;
using Products.API.Entities.DTOs;
using System.Globalization;

namespace Products.API.Endpoints;

public static class ProductEndpoints
{
    // Московский часовой пояс
    private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /products?page=1&pageSize=10 — пагинация
        app.MapGet("/products", async (
            ApplicationDbContext ctx, 
            CancellationToken ct, 
            int page = 1, 
            int pageSize = 10,
            string sortBy = "createdAt",
            string sortOrder = "desc",
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null) =>
        {
            // Валидация входных параметров 
            if(page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100)  pageSize = 10;

            // Базовый запрос
            IQueryable<Product> query = ctx.Products.AsNoTracking();

            // 1. Применяем ФИЛЬТРЫ (до пагинации и сортировки!)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchLower));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // 2. Считаем общее количество (с учётом фильтров)
            var totalCount = await query.CountAsync(ct);


            // 3. Применяем сортировку
            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("id", "asc") => query.OrderBy(p => p.Id),
                ("id", "desc") => query.OrderByDescending(p => p.Id),
                ("price", "asc") => query.OrderBy(p => p.Price),
                ("price", "desc") => query.OrderByDescending(p => p.Price),
                ("createdat", "asc") => query.OrderBy(p => p.CreatedAt),
                ("createdat", "desc") => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // 4. Применяем пагинацию
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // 5. Формируем ответ
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
                data = response,
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