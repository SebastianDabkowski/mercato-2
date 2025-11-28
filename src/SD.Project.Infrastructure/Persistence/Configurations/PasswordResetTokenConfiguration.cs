using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the PasswordResetToken entity for persistence.
/// </summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.UserId);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.UsedAt);

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
