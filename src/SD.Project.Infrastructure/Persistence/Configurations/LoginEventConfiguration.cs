using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the LoginEvent entity for persistence.
/// </summary>
public sealed class LoginEventConfiguration : IEntityTypeConfiguration<LoginEvent>
{
    public void Configure(EntityTypeBuilder<LoginEvent> builder)
    {
        builder.ToTable("LoginEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(e => e.IsSuccess)
            .IsRequired();

        builder.Property(e => e.EventType)
            .IsRequired();

        builder.Property(e => e.FailureReason)
            .HasMaxLength(500);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.Location)
            .HasMaxLength(255);

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        builder.Property(e => e.AlertTriggered)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.RetentionExpiresAt)
            .IsRequired();

        // Index on UserId for querying user's login history
        builder.HasIndex(e => e.UserId);

        // Index on Email for querying login attempts by email (including failed ones)
        builder.HasIndex(e => e.Email);

        // Index on OccurredAt for time-based queries and cleanup
        builder.HasIndex(e => e.OccurredAt);

        // Index on RetentionExpiresAt for cleanup queries
        builder.HasIndex(e => e.RetentionExpiresAt);

        // Composite index for security analysis queries
        builder.HasIndex(e => new { e.UserId, e.OccurredAt, e.IsSuccess });
    }
}
