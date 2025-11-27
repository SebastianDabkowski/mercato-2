using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the Product aggregate for persistence.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(p => p.Price, owned =>
        {
            owned.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2);
            owned.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(5);
        });

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);
    }
}
