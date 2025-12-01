using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Service for managing role-based access control permissions.
/// </summary>
public sealed class RolePermissionService
{
    private readonly ILogger<RolePermissionService> _logger;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IAuthorizationService _authorizationService;

    public RolePermissionService(
        ILogger<RolePermissionService> logger,
        IRolePermissionRepository rolePermissionRepository,
        IAuthorizationService authorizationService)
    {
        _logger = logger;
        _rolePermissionRepository = rolePermissionRepository;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Gets all role permission configurations.
    /// </summary>
    public async Task<RolePermissionConfigurationDto> HandleAsync(
        GetRolePermissionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var allPermissions = Enum.GetValues<Permission>();
        var rolePermissions = new Dictionary<UserRole, IReadOnlyCollection<Permission>>();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            var permissions = await _authorizationService.GetPermissionsForRoleAsync(role, cancellationToken);
            rolePermissions[role] = permissions;
        }

        _logger.LogInformation("Retrieved role permissions configuration");

        return new RolePermissionConfigurationDto
        {
            AllPermissions = allPermissions.ToArray(),
            AllModules = Enum.GetValues<PermissionModule>().ToArray(),
            RolePermissions = rolePermissions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value)
        };
    }

    /// <summary>
    /// Gets permissions for a specific role.
    /// </summary>
    public async Task<RolePermissionsDto> HandleAsync(
        GetRolePermissionsByRoleQuery query,
        CancellationToken cancellationToken = default)
    {
        var permissions = await _authorizationService.GetPermissionsForRoleAsync(query.Role, cancellationToken);
        var dbMappings = await _rolePermissionRepository.GetByRoleAsync(query.Role, cancellationToken);

        _logger.LogInformation(
            "Retrieved {PermissionCount} permissions for role {Role}",
            permissions.Count,
            query.Role);

        return new RolePermissionsDto
        {
            Role = query.Role,
            Permissions = permissions,
            IsCustomized = dbMappings.Count > 0
        };
    }

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    public async Task<AssignPermissionResultDto> HandleAsync(
        AssignPermissionToRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if the assignment already exists
        var existing = await _rolePermissionRepository.GetAsync(
            command.Role,
            command.Permission,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                _logger.LogInformation(
                    "Permission {Permission} is already assigned to role {Role}",
                    command.Permission,
                    command.Role);

                return new AssignPermissionResultDto
                {
                    Success = true,
                    Message = "Permission is already assigned to this role."
                };
            }

            // Reactivate the existing assignment
            existing.Activate(command.AssignedByUserId);
            await _rolePermissionRepository.UpdateAsync(existing, cancellationToken);
            await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reactivated permission {Permission} for role {Role} by user {UserId}",
                command.Permission,
                command.Role,
                command.AssignedByUserId);

            return new AssignPermissionResultDto
            {
                Success = true,
                Message = "Permission has been assigned to the role."
            };
        }

        // Create a new assignment
        var rolePermission = new RolePermission(
            command.Role,
            command.Permission,
            command.AssignedByUserId);

        await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Assigned permission {Permission} to role {Role} by user {UserId}",
            command.Permission,
            command.Role,
            command.AssignedByUserId);

        return new AssignPermissionResultDto
        {
            Success = true,
            Message = "Permission has been assigned to the role."
        };
    }

    /// <summary>
    /// Revokes a permission from a role.
    /// </summary>
    public async Task<RevokePermissionResultDto> HandleAsync(
        RevokePermissionFromRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await _rolePermissionRepository.GetAsync(
            command.Role,
            command.Permission,
            cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            _logger.LogInformation(
                "Permission {Permission} is not currently assigned to role {Role}",
                command.Permission,
                command.Role);

            return new RevokePermissionResultDto
            {
                Success = true,
                Message = "Permission is not currently assigned to this role."
            };
        }

        existing.Deactivate(command.RevokedByUserId);
        await _rolePermissionRepository.UpdateAsync(existing, cancellationToken);
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked permission {Permission} from role {Role} by user {UserId}",
            command.Permission,
            command.Role,
            command.RevokedByUserId);

        return new RevokePermissionResultDto
        {
            Success = true,
            Message = "Permission has been revoked from the role."
        };
    }

    /// <summary>
    /// Sets all permissions for a role (replaces existing).
    /// </summary>
    public async Task<SetRolePermissionsResultDto> HandleAsync(
        SetRolePermissionsCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get existing permissions for the role
        var existingMappings = await _rolePermissionRepository.GetByRoleAsync(
            command.Role,
            cancellationToken);

        var existingPermissions = existingMappings
            .Where(m => m.IsActive)
            .Select(m => m.Permission)
            .ToHashSet();

        var newPermissions = command.Permissions.ToHashSet();

        // Deactivate permissions that are no longer needed
        foreach (var mapping in existingMappings.Where(m => m.IsActive && !newPermissions.Contains(m.Permission)))
        {
            mapping.Deactivate(command.UpdatedByUserId);
            await _rolePermissionRepository.UpdateAsync(mapping, cancellationToken);
        }

        // Add or activate new permissions
        foreach (var permission in newPermissions.Except(existingPermissions))
        {
            var existingMapping = existingMappings.FirstOrDefault(m => m.Permission == permission);

            if (existingMapping is not null)
            {
                existingMapping.Activate(command.UpdatedByUserId);
                await _rolePermissionRepository.UpdateAsync(existingMapping, cancellationToken);
            }
            else
            {
                var newMapping = new RolePermission(command.Role, permission, command.UpdatedByUserId);
                await _rolePermissionRepository.AddAsync(newMapping, cancellationToken);
            }
        }

        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated permissions for role {Role} to {PermissionCount} permissions by user {UserId}",
            command.Role,
            newPermissions.Count,
            command.UpdatedByUserId);

        return new SetRolePermissionsResultDto
        {
            Success = true,
            Message = $"Role permissions updated. {newPermissions.Count} permissions are now assigned.",
            PermissionCount = newPermissions.Count
        };
    }
}
