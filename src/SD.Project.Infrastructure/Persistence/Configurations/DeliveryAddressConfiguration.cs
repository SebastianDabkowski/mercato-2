using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the DeliveryAddress entity for persistence.
/// </summary>
public sealed class DeliveryAddressConfiguration : IEntityTypeConfiguration<DeliveryAddress>
{
    public void Configure(EntityTypeBuilder<DeliveryAddress> builder)
    {
        builder.ToTable("DeliveryAddresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.BuyerId);

        builder.Property(a => a.SessionId)
            .HasMaxLength(100);

        builder.Property(a => a.Label)
            .HasMaxLength(50);

        builder.Property(a => a.RecipientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(a => a.Street)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Street2)
            .HasMaxLength(200);

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.State)
            .HasMaxLength(100);

        builder.Property(a => a.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.IsDefault)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Index for efficient buyer lookup
        builder.HasIndex(a => a.BuyerId);

        // Index for session-based lookup (guest checkout)
        builder.HasIndex(a => a.SessionId);

        // Composite index for finding default address
        builder.HasIndex(a => new { a.BuyerId, a.IsDefault, a.IsActive });
    }
}
