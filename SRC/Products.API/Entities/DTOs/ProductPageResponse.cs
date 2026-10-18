namespace Products.API.Entities.DTOs;

public record ProductPageResponse(
    IEnumerable<ProductResponseDto> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
