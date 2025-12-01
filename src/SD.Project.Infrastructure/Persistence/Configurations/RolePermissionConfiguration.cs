using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the RolePermission entity for persistence.
/// </summary>
public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Role)
            .IsRequired();

        builder.Property(rp => rp.Permission)
            .IsRequired();

        builder.Property(rp => rp.CreatedAt)
            .IsRequired();

        builder.Property(rp => rp.CreatedByUserId)
            .IsRequired();

        builder.Property(rp => rp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(rp => rp.UpdatedAt);

        builder.Property(rp => rp.UpdatedByUserId);

        // Create a unique index on Role and Permission
        builder.HasIndex(rp => new { rp.Role, rp.Permission })
            .IsUnique();

        // Index for querying active permissions by role
        builder.HasIndex(rp => new { rp.Role, rp.IsActive });
    }
}
