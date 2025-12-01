using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Central service for role-based authorization checks.
/// Provides both role-based and resource-level access control.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

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
        IRolePermissionRepository rolePermissionRepository,
        IStoreRepository storeRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository)
    {
        _logger = logger;
        _rolePermissionRepository = rolePermissionRepository;
        _storeRepository = storeRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
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

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeStoreAccessAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Authorization failure: Empty user ID attempted to access store {StoreId}", storeId);
            return AuthorizationResult.Failure("Access denied. User identification is required.");
        }

        if (storeId == Guid.Empty)
        {
            return AuthorizationResult.Failure("Access denied. Invalid store identifier.");
        }

        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Authorization failure: User {UserId} attempted to access non-existent store {StoreId}",
                userId, storeId);
            return AuthorizationResult.Failure("Access denied. Store not found.");
        }

        if (store.SellerId != userId)
        {
            _logger.LogWarning(
                "Authorization failure: User {UserId} attempted to access store {StoreId} owned by {OwnerId}",
                userId, storeId, store.SellerId);
            return AuthorizationResult.Failure("Access denied. You do not have permission to access this store.");
        }

        return AuthorizationResult.Success();
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeBuyerOrderAccessAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Authorization failure: Empty user ID attempted to access order {OrderId}", orderId);
            return AuthorizationResult.Failure("Access denied. User identification is required.");
        }

        if (orderId == Guid.Empty)
        {
            return AuthorizationResult.Failure("Access denied. Invalid order identifier.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Authorization failure: User {UserId} attempted to access non-existent order {OrderId}",
                userId, orderId);
            return AuthorizationResult.Failure("Access denied. Order not found.");
        }

        if (order.BuyerId != userId)
        {
            _logger.LogWarning(
                "Authorization failure: User {UserId} attempted to access order {OrderId} owned by {OwnerId}",
                userId, orderId, order.BuyerId);
            return AuthorizationResult.Failure("Access denied. You do not have permission to access this order.");
        }

        return AuthorizationResult.Success();
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeSellerShipmentAccessAsync(
        Guid userId,
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Authorization failure: Empty user ID attempted to access shipment {ShipmentId}", shipmentId);
            return AuthorizationResult.Failure("Access denied. User identification is required.");
        }

        if (shipmentId == Guid.Empty)
        {
            return AuthorizationResult.Failure("Access denied. Invalid shipment identifier.");
        }

        // Get the seller's store
        var store = await _storeRepository.GetBySellerIdAsync(userId, cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Authorization failure: User {UserId} has no store but attempted to access shipment {ShipmentId}",
                userId, shipmentId);
            return AuthorizationResult.Failure("Access denied. Store not found.");
        }

        // Get the shipment with order to verify ownership
        var (shipment, _, _) = await _orderRepository.GetShipmentWithOrderAsync(shipmentId, cancellationToken);
        if (shipment is null)
        {
            _logger.LogWarning("Authorization failure: User {UserId} attempted to access non-existent shipment {ShipmentId}",
                userId, shipmentId);
            return AuthorizationResult.Failure("Access denied. Shipment not found.");
        }

        if (shipment.StoreId != store.Id)
        {
            _logger.LogWarning(
                "Authorization failure: User {UserId} (store {UserStoreId}) attempted to access shipment {ShipmentId} for store {ShipmentStoreId}",
                userId, store.Id, shipmentId, shipment.StoreId);
            return AuthorizationResult.Failure("Access denied. You do not have permission to access this shipment.");
        }

        return AuthorizationResult.Success();
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> AuthorizeProductAccessAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Authorization failure: Empty user ID attempted to access product {ProductId}", productId);
            return AuthorizationResult.Failure("Access denied. User identification is required.");
        }

        if (productId == Guid.Empty)
        {
            return AuthorizationResult.Failure("Access denied. Invalid product identifier.");
        }

        // Get the seller's store
        var store = await _storeRepository.GetBySellerIdAsync(userId, cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Authorization failure: User {UserId} has no store but attempted to access product {ProductId}",
                userId, productId);
            return AuthorizationResult.Failure("Access denied. Store not found.");
        }

        // Get the product to verify ownership
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            _logger.LogWarning("Authorization failure: User {UserId} attempted to access non-existent product {ProductId}",
                userId, productId);
            return AuthorizationResult.Failure("Access denied. Product not found.");
        }

        if (product.StoreId != store.Id)
        {
            _logger.LogWarning(
                "Authorization failure: User {UserId} (store {UserStoreId}) attempted to access product {ProductId} for store {ProductStoreId}",
                userId, store.Id, productId, product.StoreId);
            return AuthorizationResult.Failure("Access denied. You do not have permission to access this product.");
        }

        return AuthorizationResult.Success();
    }

    /// <inheritdoc />
    public bool RequiresAuditLogging(UserRole userRole, SensitiveResourceType resourceType)
    {
        // Admin, Support, and Compliance roles accessing sensitive data should be audited
        if (userRole != UserRole.Admin && userRole != UserRole.Support && userRole != UserRole.Compliance)
        {
            return false;
        }

        // All sensitive resource types should be audited for admin roles
        return resourceType switch
        {
            SensitiveResourceType.CustomerProfile => true,
            SensitiveResourceType.PayoutDetails => true,
            SensitiveResourceType.OrderDetails => true,
            SensitiveResourceType.StoreDetails => true,
            SensitiveResourceType.SettlementDetails => true,
            SensitiveResourceType.KycDocuments => true,
            _ => false
        };
    }
}
