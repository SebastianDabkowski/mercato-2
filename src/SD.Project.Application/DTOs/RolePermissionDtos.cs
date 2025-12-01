using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO containing the full role permission configuration.
/// </summary>
public sealed record RolePermissionConfigurationDto
{
    /// <summary>
    /// All available permissions in the system.
    /// </summary>
    public IReadOnlyCollection<Permission> AllPermissions { get; init; } = Array.Empty<Permission>();

    /// <summary>
    /// All available modules in the system.
    /// </summary>
    public IReadOnlyCollection<PermissionModule> AllModules { get; init; } = Array.Empty<PermissionModule>();

    /// <summary>
    /// Current permissions assigned to each role.
    /// </summary>
    public IReadOnlyDictionary<UserRole, IReadOnlyCollection<Permission>> RolePermissions { get; init; }
        = new Dictionary<UserRole, IReadOnlyCollection<Permission>>();
}

/// <summary>
/// DTO containing permissions for a specific role.
/// </summary>
public sealed record RolePermissionsDto
{
    /// <summary>
    /// The role.
    /// </summary>
    public UserRole Role { get; init; }

    /// <summary>
    /// The permissions assigned to this role.
    /// </summary>
    public IReadOnlyCollection<Permission> Permissions { get; init; } = Array.Empty<Permission>();

    /// <summary>
    /// Indicates whether this role has customized permissions (vs default).
    /// </summary>
    public bool IsCustomized { get; init; }
}

/// <summary>
/// Result of assigning a permission to a role.
/// </summary>
public sealed record AssignPermissionResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Result of revoking a permission from a role.
/// </summary>
public sealed record RevokePermissionResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Result of setting all permissions for a role.
/// </summary>
public sealed record SetRolePermissionsResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int PermissionCount { get; init; }
}
