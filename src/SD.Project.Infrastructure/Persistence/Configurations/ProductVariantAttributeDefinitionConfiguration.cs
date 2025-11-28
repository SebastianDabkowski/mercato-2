using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the ProductVariantAttributeDefinition entity for persistence.
/// </summary>
public sealed class ProductVariantAttributeDefinitionConfiguration : IEntityTypeConfiguration<ProductVariantAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttributeDefinition> builder)
    {
        builder.ToTable("ProductVariantAttributeDefinitions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.ProductId)
            .IsRequired();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.PossibleValues)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.DisplayOrder)
            .IsRequired();

        builder.HasIndex(d => d.ProductId)
            .HasDatabaseName("IX_ProductVariantAttributeDefinitions_ProductId");
    }
}
