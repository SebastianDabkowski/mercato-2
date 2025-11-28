using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Application.Services;

/// <summary>
/// Central service for role-based authorization checks.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(ILogger<AuthorizationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public AuthorizationResult Authorize(UserRole userRole, params UserRole[] requiredRoles)
    {
        if (requiredRoles == null || requiredRoles.Length == 0)
        {
            return AuthorizationResult.Success();
        }

        if (requiredRoles.Contains(userRole))
        {
            return AuthorizationResult.Success();
        }

        _logger.LogWarning(
            "Authorization failure: User with role {UserRole} attempted to access feature requiring roles {RequiredRoles}",
            userRole,
            string.Join(", ", requiredRoles));

        return AuthorizationResult.Failure(
            $"Access denied. This feature requires one of the following roles: {string.Join(", ", requiredRoles)}.");
    }

    /// <inheritdoc />
    public bool CanAccessBuyerFeatures(UserRole userRole)
    {
        // Buyers and Admins can access buyer features
        return userRole == UserRole.Buyer || userRole == UserRole.Admin;
    }

    /// <inheritdoc />
    public bool CanAccessSellerFeatures(UserRole userRole)
    {
        // Sellers and Admins can access seller features
        return userRole == UserRole.Seller || userRole == UserRole.Admin;
    }

    /// <inheritdoc />
    public bool CanAccessAdminFeatures(UserRole userRole)
    {
        // Only Admins can access admin features
        return userRole == UserRole.Admin;
    }
}
