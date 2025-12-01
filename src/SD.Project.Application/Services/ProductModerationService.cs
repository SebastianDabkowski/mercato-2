using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for product moderation operations.
/// </summary>
public sealed class ProductModerationService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductModerationAuditLogRepository _auditLogRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly INotificationService _notificationService;

    public ProductModerationService(
        IProductRepository productRepository,
        IProductModerationAuditLogRepository auditLogRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IProductImageRepository productImageRepository,
        INotificationService notificationService)
    {
        _productRepository = productRepository;
        _auditLogRepository = auditLogRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _productImageRepository = productImageRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets products for the moderation queue with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<ProductModerationDto>> HandleAsync(
        GetProductsForModerationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (products, totalCount) = await _productRepository.GetByModerationStatusPagedAsync(
            query.Status,
            query.Category,
            query.SearchTerm,
            pageNumber,
            pageSize,
            cancellationToken);

        // Get store IDs and image data for the products
        var storeIds = products
            .Where(p => p.StoreId.HasValue)
            .Select(p => p.StoreId!.Value)
            .Distinct()
            .ToList();

        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Get seller info
        var sellerIds = stores.Select(s => s.SellerId).Distinct().ToList();
        var sellers = await GetUsersByIdsAsync(sellerIds, cancellationToken);
        var sellerLookup = sellers.ToDictionary(u => u.Id);

        // Get main images for products
        var productIds = products.Select(p => p.Id).ToList();
        var mainImages = await GetMainImagesAsync(productIds, cancellationToken);

        // Get moderator names
        var moderatorIds = products
            .Where(p => p.LastModeratorId.HasValue)
            .Select(p => p.LastModeratorId!.Value)
            .Distinct()
            .ToList();
        var moderators = await GetUsersByIdsAsync(moderatorIds, cancellationToken);
        var moderatorLookup = moderators.ToDictionary(u => u.Id);

        var items = products.Select(p =>
        {
            Store? store = null;
            User? seller = null;
            User? moderator = null;
            string? mainImageUrl = null;

            if (p.StoreId.HasValue && storeLookup.TryGetValue(p.StoreId.Value, out store))
            {
                sellerLookup.TryGetValue(store.SellerId, out seller);
            }

            if (p.LastModeratorId.HasValue)
            {
                moderatorLookup.TryGetValue(p.LastModeratorId.Value, out moderator);
            }

            mainImages.TryGetValue(p.Id, out mainImageUrl);

            return new ProductModerationDto(
                p.Id,
                p.StoreId,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Stock,
                p.Category,
                p.Status,
                p.ModerationStatus,
                p.ModerationRejectionReason,
                p.LastModeratorId,
                moderator != null ? $"{moderator.FirstName} {moderator.LastName}" : null,
                p.LastModeratedAt,
                store?.Name,
                seller != null ? $"{seller.FirstName} {seller.LastName}" : null,
                seller?.Email.Value,
                mainImageUrl,
                p.CreatedAt,
                p.UpdatedAt);
        }).ToList();

        return PagedResultDto<ProductModerationDto>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets moderation statistics for the dashboard.
    /// </summary>
    public async Task<ProductModerationStatsDto> HandleAsync(
        GetProductModerationStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (pendingProducts, pendingCount) = await _productRepository.GetByModerationStatusPagedAsync(
            ProductModerationStatus.PendingReview, null, null, 1, 1, cancellationToken);
        
        var (approvedProducts, approvedCount) = await _productRepository.GetByModerationStatusPagedAsync(
            ProductModerationStatus.Approved, null, null, 1, 1, cancellationToken);

        var (rejectedProducts, rejectedCount) = await _productRepository.GetByModerationStatusPagedAsync(
            ProductModerationStatus.Rejected, null, null, 1, 1, cancellationToken);

        // Count today's actions (approximated from last moderated date)
        var today = DateTime.UtcNow.Date;
        
        // Get all recently moderated products to count today's actions
        var (allApproved, _) = await _productRepository.GetByModerationStatusPagedAsync(
            ProductModerationStatus.Approved, null, null, 1, 1000, cancellationToken);
        var approvedTodayCount = allApproved.Count(p => p.LastModeratedAt?.Date == today);

        var (allRejected, _) = await _productRepository.GetByModerationStatusPagedAsync(
            ProductModerationStatus.Rejected, null, null, 1, 1000, cancellationToken);
        var rejectedTodayCount = allRejected.Count(p => p.LastModeratedAt?.Date == today);

        return new ProductModerationStatsDto(
            pendingCount,
            approvedCount,
            rejectedCount,
            approvedTodayCount,
            rejectedTodayCount);
    }

    /// <summary>
    /// Gets product details for moderation review.
    /// </summary>
    public async Task<ProductModerationDto?> HandleAsync(
        GetProductForModerationDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        Store? store = null;
        User? seller = null;
        User? moderator = null;
        string? mainImageUrl = null;

        if (product.StoreId.HasValue)
        {
            store = await _storeRepository.GetByIdAsync(product.StoreId.Value, cancellationToken);
            if (store != null)
            {
                seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
            }
        }

        if (product.LastModeratorId.HasValue)
        {
            moderator = await _userRepository.GetByIdAsync(product.LastModeratorId.Value, cancellationToken);
        }

        // Get main image
        var mainImages = await GetMainImagesAsync(new[] { product.Id }, cancellationToken);
        mainImages.TryGetValue(product.Id, out mainImageUrl);

        return new ProductModerationDto(
            product.Id,
            product.StoreId,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Stock,
            product.Category,
            product.Status,
            product.ModerationStatus,
            product.ModerationRejectionReason,
            product.LastModeratorId,
            moderator != null ? $"{moderator.FirstName} {moderator.LastName}" : null,
            product.LastModeratedAt,
            store?.Name,
            seller != null ? $"{seller.FirstName} {seller.LastName}" : null,
            seller?.Email.Value,
            mainImageUrl,
            product.CreatedAt,
            product.UpdatedAt);
    }

    /// <summary>
    /// Gets moderation audit history for a product.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductModerationAuditLogDto>> HandleAsync(
        GetProductModerationHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var auditLogs = await _auditLogRepository.GetByProductIdAsync(query.ProductId, cancellationToken);

        // Get moderator names
        var moderatorIds = auditLogs.Select(a => a.ModeratorId).Distinct().ToList();
        var moderators = await GetUsersByIdsAsync(moderatorIds, cancellationToken);
        var moderatorLookup = moderators.ToDictionary(u => u.Id);

        return auditLogs.Select(a =>
        {
            moderatorLookup.TryGetValue(a.ModeratorId, out var moderator);
            return new ProductModerationAuditLogDto(
                a.Id,
                a.ProductId,
                a.ModeratorId,
                moderator != null ? $"{moderator.FirstName} {moderator.LastName}" : null,
                a.Decision,
                a.Reason,
                a.CreatedAt);
        }).ToList();
    }

    /// <summary>
    /// Approves a product for listing.
    /// </summary>
    public async Task<ProductModerationResultDto> HandleAsync(
        ApproveProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductModerationResultDto.Failed("Product not found.");
        }

        var errors = product.ApproveModeration(command.ModeratorId);
        if (errors.Count > 0)
        {
            return ProductModerationResultDto.Failed(string.Join(" ", errors));
        }

        // Create audit log
        var auditLog = new ProductModerationAuditLog(
            product.Id,
            command.ModeratorId,
            ProductModerationStatus.Approved);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        // Send notification to seller
        await SendApprovalNotificationAsync(product, cancellationToken);

        return ProductModerationResultDto.Succeeded(product.Id);
    }

    /// <summary>
    /// Rejects a product with a reason.
    /// </summary>
    public async Task<ProductModerationResultDto> HandleAsync(
        RejectProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductModerationResultDto.Failed("Product not found.");
        }

        var errors = product.RejectModeration(command.ModeratorId, command.Reason);
        if (errors.Count > 0)
        {
            return ProductModerationResultDto.Failed(string.Join(" ", errors));
        }

        // Create audit log
        var auditLog = new ProductModerationAuditLog(
            product.Id,
            command.ModeratorId,
            ProductModerationStatus.Rejected,
            command.Reason);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        // Send notification to seller
        await SendRejectionNotificationAsync(product, cancellationToken);

        return ProductModerationResultDto.Succeeded(product.Id);
    }

    /// <summary>
    /// Batch approves multiple products.
    /// </summary>
    public async Task<BatchProductModerationResultDto> HandleAsync(
        BatchApproveProductsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductIds.Count == 0)
        {
            return BatchProductModerationResultDto.Failed(new[] { "No products specified." });
        }

        var products = await _productRepository.GetByIdsAsync(command.ProductIds, cancellationToken);
        var errors = new List<string>();
        var successCount = 0;

        foreach (var product in products)
        {
            var approvalErrors = product.ApproveModeration(command.ModeratorId);
            if (approvalErrors.Count > 0)
            {
                errors.Add($"Product '{product.Name}': {string.Join(" ", approvalErrors)}");
                continue;
            }

            var auditLog = new ProductModerationAuditLog(
                product.Id,
                command.ModeratorId,
                ProductModerationStatus.Approved);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _productRepository.Update(product);
            successCount++;

            // Send notification to seller
            await SendApprovalNotificationAsync(product, cancellationToken);
        }

        // Add errors for products not found
        var foundIds = products.Select(p => p.Id).ToHashSet();
        foreach (var id in command.ProductIds.Where(id => !foundIds.Contains(id)))
        {
            errors.Add($"Product {id} not found.");
        }

        await _productRepository.SaveChangesAsync(cancellationToken);

        if (errors.Count > 0)
        {
            return BatchProductModerationResultDto.PartialSuccess(successCount, errors.Count, errors);
        }

        return BatchProductModerationResultDto.Succeeded(successCount);
    }

    /// <summary>
    /// Batch rejects multiple products with a reason.
    /// </summary>
    public async Task<BatchProductModerationResultDto> HandleAsync(
        BatchRejectProductsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductIds.Count == 0)
        {
            return BatchProductModerationResultDto.Failed(new[] { "No products specified." });
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return BatchProductModerationResultDto.Failed(new[] { "Rejection reason is required." });
        }

        var products = await _productRepository.GetByIdsAsync(command.ProductIds, cancellationToken);
        var errors = new List<string>();
        var successCount = 0;

        foreach (var product in products)
        {
            var rejectionErrors = product.RejectModeration(command.ModeratorId, command.Reason);
            if (rejectionErrors.Count > 0)
            {
                errors.Add($"Product '{product.Name}': {string.Join(" ", rejectionErrors)}");
                continue;
            }

            var auditLog = new ProductModerationAuditLog(
                product.Id,
                command.ModeratorId,
                ProductModerationStatus.Rejected,
                command.Reason);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _productRepository.Update(product);
            successCount++;

            // Send notification to seller
            await SendRejectionNotificationAsync(product, cancellationToken);
        }

        // Add errors for products not found
        var foundIds = products.Select(p => p.Id).ToHashSet();
        foreach (var id in command.ProductIds.Where(id => !foundIds.Contains(id)))
        {
            errors.Add($"Product {id} not found.");
        }

        await _productRepository.SaveChangesAsync(cancellationToken);

        if (errors.Count > 0)
        {
            return BatchProductModerationResultDto.PartialSuccess(successCount, errors.Count, errors);
        }

        return BatchProductModerationResultDto.Succeeded(successCount);
    }

    private async Task<IReadOnlyCollection<User>> GetUsersByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var users = new List<User>();
        foreach (var id in ids.Distinct())
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user != null)
            {
                users.Add(user);
            }
        }
        return users;
    }

    private async Task<Dictionary<Guid, string>> GetMainImagesAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        foreach (var productId in productIds)
        {
            var images = await _productImageRepository.GetByProductIdAsync(productId, cancellationToken);
            var mainImage = images.FirstOrDefault(i => i.IsMain) ?? images.FirstOrDefault();
            if (mainImage != null)
            {
                result[productId] = mainImage.ImageUrl;
            }
        }
        return result;
    }

    private async Task SendApprovalNotificationAsync(Product product, CancellationToken cancellationToken)
    {
        if (!product.StoreId.HasValue) return;

        var store = await _storeRepository.GetByIdAsync(product.StoreId.Value, cancellationToken);
        if (store is null) return;

        var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
        if (seller is null) return;

        await _notificationService.SendProductApprovedAsync(
            product.Id,
            product.Name,
            seller.Email.Value,
            cancellationToken);
    }

    private async Task SendRejectionNotificationAsync(Product product, CancellationToken cancellationToken)
    {
        if (!product.StoreId.HasValue) return;
        if (string.IsNullOrEmpty(product.ModerationRejectionReason)) return;

        var store = await _storeRepository.GetByIdAsync(product.StoreId.Value, cancellationToken);
        if (store is null) return;

        var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
        if (seller is null) return;

        await _notificationService.SendProductRejectedAsync(
            product.Id,
            product.Name,
            seller.Email.Value,
            product.ModerationRejectionReason,
            cancellationToken);
    }
}
