using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for role permissions.
/// </summary>
public sealed class RolePermissionRepository : IRolePermissionRepository
{
    private readonly AppDbContext _context;

    public RolePermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Permission>> GetPermissionsForRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role == role && rp.IsActive)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        return permissions.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RolePermission>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.IsActive)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RolePermission>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role == role)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<RolePermission?> GetAsync(
        UserRole role,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == role && rp.Permission == permission, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default)
    {
        await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default)
    {
        _context.RolePermissions.Update(rolePermission);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        UserRole role,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .AnyAsync(rp => rp.Role == role && rp.Permission == permission && rp.IsActive, cancellationToken);
    }
}
