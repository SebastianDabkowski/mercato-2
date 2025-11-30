using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for SellerPayout entity.
/// </summary>
public class SellerPayoutConfiguration : IEntityTypeConfiguration<SellerPayout>
{
    public void Configure(EntityTypeBuilder<SellerPayout> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.StoreId)
            .IsRequired();

        builder.Property(p => p.SellerId)
            .IsRequired();

        builder.Property(p => p.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.ScheduledDate)
            .IsRequired();

        builder.Property(p => p.PayoutMethod)
            .IsRequired();

        builder.Property(p => p.PayoutReference)
            .HasMaxLength(256);

        builder.Property(p => p.ErrorReference)
            .HasMaxLength(256);

        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(1024);

        builder.Property(p => p.RetryCount)
            .IsRequired();

        builder.Property(p => p.MaxRetries)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(p => p.StoreId);
        builder.HasIndex(p => p.SellerId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ScheduledDate);
        builder.HasIndex(p => new { p.Status, p.ScheduledDate });
        builder.HasIndex(p => new { p.Status, p.NextRetryAt });

        // Ignore the Items navigation property - handled separately
        builder.Ignore(p => p.Items);
    }
}

/// <summary>
/// EF Core configuration for SellerPayoutItem entity.
/// </summary>
public class SellerPayoutItemConfiguration : IEntityTypeConfiguration<SellerPayoutItem>
{
    public void Configure(EntityTypeBuilder<SellerPayoutItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SellerPayoutId)
            .IsRequired();

        builder.Property(i => i.EscrowAllocationId)
            .IsRequired();

        builder.Property(i => i.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(i => i.SellerPayoutId);

        // Unique constraint - each allocation can only be in one payout
        builder.HasIndex(i => i.EscrowAllocationId)
            .IsUnique();
    }
}
