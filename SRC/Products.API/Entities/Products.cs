using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Products.API.Entities;

[Table("products")]
public class Products
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("price")]
    [Required]
    [Precision(10, 2)]
    public decimal Price { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; }
}
