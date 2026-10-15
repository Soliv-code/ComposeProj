using System.ComponentModel.DataAnnotations;

namespace Products.API.Entities.DTOs;

public record CreateProductDto(
    //[property: Required(ErrorMessage = "Название товара обязательно")]
    //[property: MaxLength(255, ErrorMessage = "Название не может быть длиннее 255 символов")]
    [Required(ErrorMessage = "Название товара обязательно")]
    [MaxLength(255)]
    string Name,
    
    //[property: Required(ErrorMessage = "Цена обязательна")]
    //[property: Range(0, (double)decimal.MaxValue, ErrorMessage = "Цена не может быть отрицательной")]
    [Required]
    [Range(0, (double)decimal.MaxValue)]
    decimal Price,

    string? Description = null
);