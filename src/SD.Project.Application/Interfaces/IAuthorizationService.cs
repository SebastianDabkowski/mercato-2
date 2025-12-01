using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Defines the contract for authorization checks in the application.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks whether a user with the given role is authorized to access a specific feature.
    /// </summary>
    /// <param name="userRole">The role of the user attempting access.</param>
    /// <param name="requiredRoles">The roles that are allowed to access the feature.</param>
    /// <returns>An authorization result indicating success or failure with a message.</returns>
    AuthorizationResult Authorize(UserRole userRole, params UserRole[] requiredRoles);

    /// <summary>
    /// Checks whether a user with the given role can access buyer features.
    /// </summary>
    bool CanAccessBuyerFeatures(UserRole userRole);

    /// <summary>
    /// Checks whether a user with the given role can access seller features.
    /// </summary>
    bool CanAccessSellerFeatures(UserRole userRole);

    /// <summary>
    /// Checks whether a user with the given role can access admin features.
    /// </summary>
    bool CanAccessAdminFeatures(UserRole userRole);

    /// <summary>
    /// Checks whether a user with the given role has a specific permission.
    /// </summary>
    /// <param name="userRole">The role of the user.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(
        UserRole userRole,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user with the given role has any of the specified permissions.
    /// </summary>
    /// <param name="userRole">The role of the user.</param>
    /// <param name="permissions">The permissions to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has any of the permissions, false otherwise.</returns>
    Task<bool> HasAnyPermissionAsync(
        UserRole userRole,
        IEnumerable<Permission> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a specific role.
    /// </summary>
    /// <param name="userRole">The role to get permissions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of permissions for the role.</returns>
    Task<IReadOnlyCollection<Permission>> GetPermissionsForRoleAsync(
        UserRole userRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes access based on a required permission.
    /// </summary>
    /// <param name="userRole">The role of the user attempting access.</param>
    /// <param name="permission">The required permission.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authorization result indicating success or failure with a message.</returns>
    Task<AuthorizationResult> AuthorizePermissionAsync(
        UserRole userRole,
        Permission permission,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of an authorization check.
/// </summary>
public sealed record AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthorizationResult Success() => new() { IsAuthorized = true };

    public static AuthorizationResult Failure(string message) =>
        new() { IsAuthorized = false, ErrorMessage = message };
}
