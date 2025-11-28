using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the UserSession entity for persistence.
/// </summary>
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.Token)
            .IsRequired()
            .HasMaxLength(100);

        // Index on Token for fast lookups during session validation
        builder.HasIndex(s => s.Token)
            .IsUnique();

        // Index on UserId for finding all sessions for a user
        builder.HasIndex(s => s.UserId);

        // Composite index for cleanup queries (finding expired/revoked sessions)
        builder.HasIndex(s => new { s.ExpiresAt, s.RevokedAt });

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        builder.Property(s => s.LastActivityAt)
            .IsRequired();

        builder.Property(s => s.RevokedAt);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(512);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(s => s.IsPersistent)
            .IsRequired();
    }
}
