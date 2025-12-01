using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to assign a permission to a role.
/// </summary>
public sealed record AssignPermissionToRoleCommand(
    UserRole Role,
    Permission Permission,
    Guid AssignedByUserId);

/// <summary>
/// Command to revoke a permission from a role.
/// </summary>
public sealed record RevokePermissionFromRoleCommand(
    UserRole Role,
    Permission Permission,
    Guid RevokedByUserId);

/// <summary>
/// Command to set all permissions for a role (replaces existing).
/// </summary>
public sealed record SetRolePermissionsCommand(
    UserRole Role,
    IEnumerable<Permission> Permissions,
    Guid UpdatedByUserId);
