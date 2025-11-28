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
