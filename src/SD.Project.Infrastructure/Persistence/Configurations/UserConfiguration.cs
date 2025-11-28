using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the User entity for persistence.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .IsRequired()
            .HasMaxLength(254);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Role)
            .IsRequired();

        builder.Property(u => u.Status)
            .IsRequired();

        builder.Property(u => u.ExternalProvider)
            .IsRequired();

        builder.Property(u => u.ExternalId)
            .HasMaxLength(255);

        // Create an index on external provider and ID for efficient lookup
        builder.HasIndex(u => new { u.ExternalProvider, u.ExternalId })
            .HasFilter("[ExternalProvider] <> 0"); // Only index when there's an external provider

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.CompanyName)
            .HasMaxLength(200);

        builder.Property(u => u.TaxId)
            .HasMaxLength(50);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(u => u.AcceptedTerms)
            .IsRequired();

        builder.Property(u => u.AcceptedTermsAt)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
