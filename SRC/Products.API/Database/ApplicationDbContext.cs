using Microsoft.EntityFrameworkCore;
using Products.API.Entities;


namespace Products.API.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opt) : base(opt)
    {
    }

    public DbSet<Entities.Products> Products { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Products>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Price)
                .HasColumnName("price")
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .ValueGeneratedOnAddOrUpdate();
        });
    }

}
