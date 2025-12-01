using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all role permission configurations.
/// </summary>
public sealed record GetRolePermissionsQuery;

/// <summary>
/// Query to get permissions for a specific role.
/// </summary>
public sealed record GetRolePermissionsByRoleQuery(UserRole Role);
