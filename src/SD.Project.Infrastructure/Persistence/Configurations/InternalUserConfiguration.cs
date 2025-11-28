using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the InternalUser entity for persistence.
/// </summary>
public sealed class InternalUserConfiguration : IEntityTypeConfiguration<InternalUser>
{
    public void Configure(EntityTypeBuilder<InternalUser> builder)
    {
        builder.ToTable("InternalUsers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoreId)
            .IsRequired();

        builder.Property(x => x.UserId);

        builder.Property(x => x.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(x => x.Role)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.ActivatedAt);

        builder.Property(x => x.DeactivatedAt);

        builder.Property(x => x.InvitedByUserId)
            .IsRequired();

        // Create index on StoreId for efficient store user lookups
        builder.HasIndex(x => x.StoreId);

        // Create index on StoreId and UserId for permission lookups
        builder.HasIndex(x => new { x.StoreId, x.UserId });

        // Create unique index on StoreId and Email to prevent duplicate emails per store
        builder.HasIndex(x => new { x.StoreId, x.Email })
            .IsUnique();
    }
}
