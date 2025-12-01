using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Central service for role-based authorization checks.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IRolePermissionRepository _rolePermissionRepository;

    // Default permissions for each role (used when no database permissions are configured)
    private static readonly Dictionary<UserRole, HashSet<Permission>> DefaultPermissions = new()
    {
        [UserRole.Buyer] = new HashSet<Permission>
        {
            Permission.ProductView,
            Permission.OrderView,
            Permission.ReturnView,
            Permission.ReviewView,
            Permission.ReviewCreate,
            Permission.CategoryView
        },
        [UserRole.Seller] = new HashSet<Permission>
        {
            Permission.ProductView,
            Permission.ProductCreate,
            Permission.ProductEdit,
            Permission.ProductDelete,
            Permission.ProductImport,
            Permission.ProductExport,
            Permission.OrderView,
            Permission.OrderProcess,
            Permission.OrderCancel,
            Permission.OrderExport,
            Permission.ReturnView,
            Permission.ReturnProcess,
            Permission.StoreView,
            Permission.StoreEdit,
            Permission.StoreManageTeam,
            Permission.PaymentView,
            Permission.SettlementView,
            Permission.CommissionView,
            Permission.ReviewView,
            Permission.ReportDashboard,
            Permission.ReportSales,
            Permission.ReportExport,
            Permission.CategoryView,
            Permission.ShippingManage
        },
        [UserRole.Admin] = Enum.GetValues<Permission>().ToHashSet(), // Admin has all permissions
        [UserRole.Support] = new HashSet<Permission>
        {
            Permission.ProductView,
            Permission.OrderView,
            Permission.OrderViewAll,
            Permission.ReturnView,
            Permission.ReturnProcess,
            Permission.ReturnEscalate,
            Permission.UserView,
            Permission.UserEdit,
            Permission.StoreView,
            Permission.PaymentView,
            Permission.ReviewView,
            Permission.ReviewModerate,
            Permission.ReportDashboard,
            Permission.CategoryView
        },
        [UserRole.Compliance] = new HashSet<Permission>
        {
            Permission.ProductView,
            Permission.ProductModerate,
            Permission.OrderView,
            Permission.OrderViewAll,
            Permission.ReturnView,
            Permission.ReturnResolve,
            Permission.UserView,
            Permission.UserBlock,
            Permission.StoreView,
            Permission.StoreApproveOnboarding,
            Permission.ReviewView,
            Permission.ReviewModerate,
            Permission.ReportDashboard,
            Permission.ComplianceView,
            Permission.ComplianceKycReview,
            Permission.ComplianceDataProcessing,
            Permission.ComplianceAuditLog
        }
    };

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        IRolePermissionRepository rolePermissionRepository)
    {
        _logger = logger;
        _rolePermissionRepository = rolePermissionRepository;
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
        // Buyers, Support, and Admins can access buyer features
        return userRole == UserRole.Buyer || userRole == UserRole.Admin || userRole == UserRole.Support;
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
        // Admins, Support, and Compliance can access admin features (with varying permissions)
        return userRole == UserRole.Admin || userRole == UserRole.Support || userRole == UserRole.Compliance;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        UserRole userRole,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        // Check database permissions first
        var hasDbPermission = await _rolePermissionRepository.HasPermissionAsync(
            userRole,
            permission,
            cancellationToken);

        if (hasDbPermission)
        {
            return true;
        }

        // Fall back to default permissions if no database permissions exist
        var dbPermissions = await _rolePermissionRepository.GetPermissionsForRoleAsync(
            userRole,
            cancellationToken);

        // If there are any database permissions for this role, use only those
        if (dbPermissions.Count > 0)
        {
            return false;
        }

        // Otherwise, use default permissions
        return DefaultPermissions.TryGetValue(userRole, out var defaults) &&
               defaults.Contains(permission);
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyPermissionAsync(
        UserRole userRole,
        IEnumerable<Permission> permissions,
        CancellationToken cancellationToken = default)
    {
        foreach (var permission in permissions)
        {
            if (await HasPermissionAsync(userRole, permission, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Permission>> GetPermissionsForRoleAsync(
        UserRole userRole,
        CancellationToken cancellationToken = default)
    {
        // Check database permissions first
        var dbPermissions = await _rolePermissionRepository.GetPermissionsForRoleAsync(
            userRole,
            cancellationToken);

        // If there are database permissions, return those
        if (dbPermissions.Count > 0)
        {
            return dbPermissions;
        }

        // Otherwise, return default permissions
        if (DefaultPermissions.TryGetValue(userRole, out var defaults))
        {
            return defaults;
        }

        return Array.Empty<Permission>();
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizePermissionAsync(
        UserRole userRole,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var hasPermission = await HasPermissionAsync(userRole, permission, cancellationToken);

        if (hasPermission)
        {
            return AuthorizationResult.Success();
        }

        _logger.LogWarning(
            "Authorization failure: User with role {UserRole} attempted to access feature requiring permission {Permission}",
            userRole,
            permission);

        return AuthorizationResult.Failure(
            $"Access denied. You do not have the required permission: {permission}.");
    }
}
