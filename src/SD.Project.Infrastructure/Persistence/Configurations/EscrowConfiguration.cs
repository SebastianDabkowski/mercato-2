using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for escrow entities.
/// </summary>
public class EscrowPaymentConfiguration : IEntityTypeConfiguration<EscrowPayment>
{
    public void Configure(EntityTypeBuilder<EscrowPayment> builder)
    {
        builder.ToTable("EscrowPayments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.ReleasedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.RefundedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.PaymentTransactionId)
            .HasMaxLength(256);

        builder.HasIndex(e => e.OrderId).IsUnique();
        builder.HasIndex(e => e.BuyerId);
        builder.HasIndex(e => e.Status);
    }
}

/// <summary>
/// EF Core configuration for escrow allocation entities.
/// </summary>
public class EscrowAllocationConfiguration : IEntityTypeConfiguration<EscrowAllocation>
{
    public void Configure(EntityTypeBuilder<EscrowAllocation> builder)
    {
        builder.ToTable("EscrowAllocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SellerAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.ShippingAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.CommissionAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.CommissionRate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(a => a.SellerPayout)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.RefundedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.RefundedSellerAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.RefundedCommissionAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.PayoutReference)
            .HasMaxLength(256);

        builder.Property(a => a.RefundReference)
            .HasMaxLength(256);

        builder.HasIndex(a => a.EscrowPaymentId);
        builder.HasIndex(a => a.ShipmentId).IsUnique();
        builder.HasIndex(a => a.StoreId);
        builder.HasIndex(a => a.Status);
    }
}

/// <summary>
/// EF Core configuration for escrow ledger entities.
/// </summary>
public class EscrowLedgerConfiguration : IEntityTypeConfiguration<EscrowLedger>
{
    public void Configure(EntityTypeBuilder<EscrowLedger> builder)
    {
        builder.ToTable("EscrowLedgers");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(l => l.ExternalReference)
            .HasMaxLength(256);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.Property(l => l.InitiatedBy)
            .HasMaxLength(256);

        builder.HasIndex(l => l.EscrowPaymentId);
        builder.HasIndex(l => l.AllocationId);
        builder.HasIndex(l => l.OrderId);
        builder.HasIndex(l => l.Action);
        builder.HasIndex(l => l.CreatedAt);
    }
}
