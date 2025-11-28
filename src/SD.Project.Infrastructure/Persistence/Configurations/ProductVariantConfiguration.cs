using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the ProductVariant entity for persistence.
/// </summary>
public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProductId)
            .IsRequired();

        builder.Property(v => v.Sku)
            .HasMaxLength(100);

        builder.Property(v => v.Stock)
            .IsRequired();

        builder.OwnsOne(v => v.PriceOverride, owned =>
        {
            owned.Property(m => m.Amount)
                .HasColumnName("PriceOverrideAmount")
                .HasPrecision(18, 2);
            owned.Property(m => m.Currency)
                .HasColumnName("PriceOverrideCurrency")
                .HasMaxLength(5);
        });

        builder.Property(v => v.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(v => v.AttributeValues)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(v => new { v.ProductId, v.Sku })
            .HasDatabaseName("IX_ProductVariants_ProductId_Sku");
    }
}
