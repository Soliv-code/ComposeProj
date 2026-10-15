namespace Products.API.Entities.DTOs;

public record ProductResponseDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string CreatedAt,
    string UpdatedAt
);
