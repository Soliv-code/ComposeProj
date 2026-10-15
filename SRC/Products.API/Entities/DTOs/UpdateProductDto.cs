using System.ComponentModel.DataAnnotations;

namespace Products.API.Entities.DTOs;

public record UpdateProductDto(

    [Required(ErrorMessage = "Название товара обязательно")]
    [MaxLength(255, ErrorMessage = "Название не может быть длиннее 255 символов")]
    string Name,

    [Required(ErrorMessage = "Цена обязательна")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Цена не может быть отрицательной")]
    decimal Price,

    string? Description = null
);
