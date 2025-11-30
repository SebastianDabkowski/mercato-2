using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the CommissionRule entity.
/// </summary>
public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.ToTable("CommissionRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RuleType)
            .IsRequired();

        builder.Property(r => r.CategoryId);

        builder.Property(r => r.StoreId);

        builder.Property(r => r.CommissionRate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.EffectiveFrom);

        builder.Property(r => r.EffectiveTo);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        // Indexes for efficient lookups
        builder.HasIndex(r => r.RuleType);
        builder.HasIndex(r => r.CategoryId).HasFilter("[CategoryId] IS NOT NULL");
        builder.HasIndex(r => r.StoreId).HasFilter("[StoreId] IS NOT NULL");
        builder.HasIndex(r => new { r.RuleType, r.IsActive });
        builder.HasIndex(r => new { r.RuleType, r.CategoryId, r.IsActive })
            .HasFilter("[CategoryId] IS NOT NULL");
        builder.HasIndex(r => new { r.RuleType, r.StoreId, r.IsActive })
            .HasFilter("[StoreId] IS NOT NULL");
    }
}
