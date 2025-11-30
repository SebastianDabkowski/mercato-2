using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for refund entities.
/// </summary>
public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.CommissionRefundAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.OriginalTransactionId)
            .HasMaxLength(256);

        builder.Property(r => r.RefundTransactionId)
            .HasMaxLength(256);

        builder.Property(r => r.IdempotencyKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(r => r.InitiatorType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(r => r.ErrorCode)
            .HasMaxLength(100);

        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.ShipmentId);
        builder.HasIndex(r => r.BuyerId);
        builder.HasIndex(r => r.StoreId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.IdempotencyKey).IsUnique();
        builder.HasIndex(r => r.CreatedAt);
    }
}
