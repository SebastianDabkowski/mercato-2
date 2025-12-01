using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VatRule entity.
/// </summary>
public class VatRuleConfiguration : IEntityTypeConfiguration<VatRule>
{
    public void Configure(EntityTypeBuilder<VatRule> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.TaxRate)
            .HasPrecision(5, 2);

        builder.HasIndex(e => e.CountryCode);
        builder.HasIndex(e => e.CategoryId);
        builder.HasIndex(e => new { e.CountryCode, e.CategoryId, e.IsActive });
    }
}

/// <summary>
/// EF Core configuration for VatRuleHistory entity.
/// </summary>
public class VatRuleHistoryConfiguration : IEntityTypeConfiguration<VatRuleHistory>
{
    public void Configure(EntityTypeBuilder<VatRuleHistory> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ChangedByUserName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ChangeReason)
            .HasMaxLength(500);

        builder.Property(e => e.TaxRate)
            .HasPrecision(5, 2);

        builder.HasIndex(e => e.VatRuleId);
        builder.HasIndex(e => e.ChangedByUserId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
