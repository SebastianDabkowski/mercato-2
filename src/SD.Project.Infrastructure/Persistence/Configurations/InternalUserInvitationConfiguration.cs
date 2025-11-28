using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the InternalUserInvitation entity for persistence.
/// </summary>
public sealed class InternalUserInvitationConfiguration : IEntityTypeConfiguration<InternalUserInvitation>
{
    public void Configure(EntityTypeBuilder<InternalUserInvitation> builder)
    {
        builder.ToTable("InternalUserInvitations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InternalUserId)
            .IsRequired();

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.AcceptedAt);

        // Create unique index on Token for efficient lookup
        builder.HasIndex(x => x.Token)
            .IsUnique();

        // Create index on InternalUserId for lookups by internal user
        builder.HasIndex(x => x.InternalUserId);
    }
}
