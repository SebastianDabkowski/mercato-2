using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for managing role permission assignments.
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Gets all active permissions for a specific role.
    /// </summary>
    /// <param name="role">The role to get permissions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active permissions for the role.</returns>
    Task<IReadOnlyCollection<Permission>> GetPermissionsForRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active role-permission mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all active role permission entities.</returns>
    Task<IReadOnlyCollection<RolePermission>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all role-permission mappings for a specific role.
    /// </summary>
    /// <param name="role">The role to get mappings for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of role permission entities for the role.</returns>
    Task<IReadOnlyCollection<RolePermission>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific role-permission mapping.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="permission">The permission.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role permission entity if found, null otherwise.</returns>
    Task<RolePermission?> GetAsync(
        UserRole role,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role-permission mapping.
    /// </summary>
    /// <param name="rolePermission">The role permission to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role-permission mapping.
    /// </summary>
    /// <param name="rolePermission">The role permission to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role has a specific permission.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <param name="permission">The permission to check for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(
        UserRole role,
        Permission permission,
        CancellationToken cancellationToken = default);
}
