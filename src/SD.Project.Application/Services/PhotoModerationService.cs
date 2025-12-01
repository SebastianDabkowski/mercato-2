using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for photo moderation operations.
/// </summary>
public sealed class PhotoModerationService
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IPhotoModerationAuditLogRepository _auditLogRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public PhotoModerationService(
        IProductImageRepository imageRepository,
        IPhotoModerationAuditLogRepository auditLogRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _imageRepository = imageRepository;
        _auditLogRepository = auditLogRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets photos for the moderation queue with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<PhotoModerationDto>> HandleAsync(
        GetPhotosForModerationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (photos, totalCount) = await _imageRepository.GetByModerationStatusPagedAsync(
            query.Status,
            query.IsFlagged,
            query.SearchTerm,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = await MapToModerationDtosAsync(photos, cancellationToken);

        return PagedResultDto<PhotoModerationDto>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets moderation statistics for the dashboard.
    /// </summary>
    public async Task<PhotoModerationStatsDto> HandleAsync(
        GetPhotoModerationStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (pendingPhotos, pendingCount) = await _imageRepository.GetByModerationStatusPagedAsync(
            PhotoModerationStatus.PendingReview, null, null, 1, 1, cancellationToken);

        var (flaggedPhotos, flaggedCount) = await _imageRepository.GetByModerationStatusPagedAsync(
            null, true, null, 1, 1, cancellationToken);

        var (approvedPhotos, approvedCount) = await _imageRepository.GetByModerationStatusPagedAsync(
            PhotoModerationStatus.Approved, null, null, 1, 1, cancellationToken);

        var (removedPhotos, removedCount) = await _imageRepository.GetByModerationStatusPagedAsync(
            PhotoModerationStatus.Removed, null, null, 1, 1, cancellationToken);

        // Count today's actions
        var today = DateTime.UtcNow.Date;

        var (allApproved, _) = await _imageRepository.GetByModerationStatusPagedAsync(
            PhotoModerationStatus.Approved, null, null, 1, 1000, cancellationToken);
        var approvedTodayCount = allApproved.Count(p => p.LastModeratedAt?.Date == today);

        var (allRemoved, _) = await _imageRepository.GetByModerationStatusPagedAsync(
            PhotoModerationStatus.Removed, null, null, 1, 1000, cancellationToken);
        var removedTodayCount = allRemoved.Count(p => p.LastModeratedAt?.Date == today);

        return new PhotoModerationStatsDto(
            pendingCount,
            flaggedCount,
            approvedCount,
            removedCount,
            approvedTodayCount,
            removedTodayCount);
    }

    /// <summary>
    /// Gets photo details for moderation review.
    /// </summary>
    public async Task<PhotoModerationDto?> HandleAsync(
        GetPhotoForModerationDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var photo = await _imageRepository.GetByIdAsync(query.PhotoId, cancellationToken);
        if (photo is null)
        {
            return null;
        }

        var items = await MapToModerationDtosAsync(new[] { photo }, cancellationToken);
        return items.FirstOrDefault();
    }

    /// <summary>
    /// Gets moderation audit history for a photo.
    /// </summary>
    public async Task<IReadOnlyCollection<PhotoModerationAuditLogDto>> HandleAsync(
        GetPhotoModerationHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var auditLogs = await _auditLogRepository.GetByPhotoIdAsync(query.PhotoId, cancellationToken);

        // Get moderator names
        var moderatorIds = auditLogs.Select(a => a.ModeratorId).Distinct().ToList();
        var moderators = await GetUsersByIdsAsync(moderatorIds, cancellationToken);
        var moderatorLookup = moderators.ToDictionary(u => u.Id);

        return auditLogs.Select(a =>
        {
            moderatorLookup.TryGetValue(a.ModeratorId, out var moderator);
            return new PhotoModerationAuditLogDto(
                a.Id,
                a.PhotoId,
                a.ModeratorId,
                moderator != null ? $"{moderator.FirstName} {moderator.LastName}" : null,
                a.Decision,
                a.Reason,
                a.CreatedAt);
        }).ToList();
    }

    /// <summary>
    /// Approves a photo for display.
    /// </summary>
    public async Task<PhotoModerationResultDto> HandleAsync(
        ApprovePhotoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var photo = await _imageRepository.GetByIdAsync(command.PhotoId, cancellationToken);
        if (photo is null)
        {
            return PhotoModerationResultDto.Failed("Photo not found.");
        }

        var errors = photo.ApproveModeration(command.ModeratorId);
        if (errors.Count > 0)
        {
            return PhotoModerationResultDto.Failed(string.Join(" ", errors));
        }

        // Create audit log
        var auditLog = new PhotoModerationAuditLog(
            photo.Id,
            command.ModeratorId,
            PhotoModerationStatus.Approved);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        _imageRepository.Update(photo);
        await _imageRepository.SaveChangesAsync(cancellationToken);

        return PhotoModerationResultDto.Succeeded(photo.Id);
    }

    /// <summary>
    /// Removes a photo with a reason.
    /// </summary>
    public async Task<PhotoModerationResultDto> HandleAsync(
        RemovePhotoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var photo = await _imageRepository.GetByIdAsync(command.PhotoId, cancellationToken);
        if (photo is null)
        {
            return PhotoModerationResultDto.Failed("Photo not found.");
        }

        var errors = photo.RemoveModeration(command.ModeratorId, command.Reason);
        if (errors.Count > 0)
        {
            return PhotoModerationResultDto.Failed(string.Join(" ", errors));
        }

        // If this was the main image, we need to set another visible image as main
        if (photo.IsMain)
        {
            var productImages = await _imageRepository.GetVisibleByProductIdAsync(photo.ProductId, cancellationToken);
            var nextMainImage = productImages.FirstOrDefault(i => i.Id != photo.Id);
            if (nextMainImage != null)
            {
                nextMainImage.SetAsMain();
                _imageRepository.Update(nextMainImage);
            }
        }

        // Create audit log
        var auditLog = new PhotoModerationAuditLog(
            photo.Id,
            command.ModeratorId,
            PhotoModerationStatus.Removed,
            command.Reason);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        _imageRepository.Update(photo);
        await _imageRepository.SaveChangesAsync(cancellationToken);

        // Send notification to seller
        await SendRemovalNotificationAsync(photo, cancellationToken);

        return PhotoModerationResultDto.Succeeded(photo.Id);
    }

    /// <summary>
    /// Flags a photo for moderation review.
    /// </summary>
    public async Task<PhotoModerationResultDto> HandleAsync(
        FlagPhotoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var photo = await _imageRepository.GetByIdAsync(command.PhotoId, cancellationToken);
        if (photo is null)
        {
            return PhotoModerationResultDto.Failed("Photo not found.");
        }

        var errors = photo.Flag(command.Reason);
        if (errors.Count > 0)
        {
            return PhotoModerationResultDto.Failed(string.Join(" ", errors));
        }

        _imageRepository.Update(photo);
        await _imageRepository.SaveChangesAsync(cancellationToken);

        return PhotoModerationResultDto.Succeeded(photo.Id);
    }

    /// <summary>
    /// Batch approves multiple photos.
    /// </summary>
    public async Task<BatchPhotoModerationResultDto> HandleAsync(
        BatchApprovePhotosCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PhotoIds.Count == 0)
        {
            return BatchPhotoModerationResultDto.Failed(new[] { "No photos specified." });
        }

        var photos = await _imageRepository.GetByIdsAsync(command.PhotoIds, cancellationToken);
        var errors = new List<string>();
        var successCount = 0;

        foreach (var photo in photos)
        {
            var approvalErrors = photo.ApproveModeration(command.ModeratorId);
            if (approvalErrors.Count > 0)
            {
                errors.Add($"Photo '{photo.FileName}': {string.Join(" ", approvalErrors)}");
                continue;
            }

            var auditLog = new PhotoModerationAuditLog(
                photo.Id,
                command.ModeratorId,
                PhotoModerationStatus.Approved);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _imageRepository.Update(photo);
            successCount++;
        }

        // Add errors for photos not found
        var foundIds = photos.Select(p => p.Id).ToHashSet();
        foreach (var id in command.PhotoIds.Where(id => !foundIds.Contains(id)))
        {
            errors.Add($"Photo {id} not found.");
        }

        await _imageRepository.SaveChangesAsync(cancellationToken);

        if (errors.Count > 0)
        {
            return BatchPhotoModerationResultDto.PartialSuccess(successCount, errors.Count, errors);
        }

        return BatchPhotoModerationResultDto.Succeeded(successCount);
    }

    /// <summary>
    /// Batch removes multiple photos with a reason.
    /// </summary>
    public async Task<BatchPhotoModerationResultDto> HandleAsync(
        BatchRemovePhotosCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PhotoIds.Count == 0)
        {
            return BatchPhotoModerationResultDto.Failed(new[] { "No photos specified." });
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return BatchPhotoModerationResultDto.Failed(new[] { "Removal reason is required." });
        }

        var photos = await _imageRepository.GetByIdsAsync(command.PhotoIds, cancellationToken);
        var errors = new List<string>();
        var successCount = 0;

        foreach (var photo in photos)
        {
            var removalErrors = photo.RemoveModeration(command.ModeratorId, command.Reason);
            if (removalErrors.Count > 0)
            {
                errors.Add($"Photo '{photo.FileName}': {string.Join(" ", removalErrors)}");
                continue;
            }

            var auditLog = new PhotoModerationAuditLog(
                photo.Id,
                command.ModeratorId,
                PhotoModerationStatus.Removed,
                command.Reason);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _imageRepository.Update(photo);
            successCount++;

            // Send notification to seller
            await SendRemovalNotificationAsync(photo, cancellationToken);
        }

        // Add errors for photos not found
        var foundIds = photos.Select(p => p.Id).ToHashSet();
        foreach (var id in command.PhotoIds.Where(id => !foundIds.Contains(id)))
        {
            errors.Add($"Photo {id} not found.");
        }

        await _imageRepository.SaveChangesAsync(cancellationToken);

        if (errors.Count > 0)
        {
            return BatchPhotoModerationResultDto.PartialSuccess(successCount, errors.Count, errors);
        }

        return BatchPhotoModerationResultDto.Succeeded(successCount);
    }

    private async Task<IReadOnlyList<PhotoModerationDto>> MapToModerationDtosAsync(
        IEnumerable<ProductImage> photos,
        CancellationToken cancellationToken)
    {
        var photoList = photos.ToList();
        if (photoList.Count == 0)
        {
            return Array.Empty<PhotoModerationDto>();
        }

        // Get product info
        var productIds = photoList.Select(p => p.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        // Get store IDs
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

        // Get moderator names
        var moderatorIds = photoList
            .Where(p => p.LastModeratorId.HasValue)
            .Select(p => p.LastModeratorId!.Value)
            .Distinct()
            .ToList();
        var moderators = await GetUsersByIdsAsync(moderatorIds, cancellationToken);
        var moderatorLookup = moderators.ToDictionary(u => u.Id);

        return photoList.Select(p =>
        {
            Product? product = null;
            Store? store = null;
            User? seller = null;
            User? moderator = null;

            if (productLookup.TryGetValue(p.ProductId, out product) && product.StoreId.HasValue)
            {
                if (storeLookup.TryGetValue(product.StoreId.Value, out store))
                {
                    sellerLookup.TryGetValue(store.SellerId, out seller);
                }
            }

            if (p.LastModeratorId.HasValue)
            {
                moderatorLookup.TryGetValue(p.LastModeratorId.Value, out moderator);
            }

            return new PhotoModerationDto(
                p.Id,
                p.ProductId,
                product?.StoreId,
                p.FileName,
                p.ImageUrl,
                p.ThumbnailUrl,
                p.ModerationStatus,
                p.ModerationRemovalReason,
                p.IsFlagged,
                p.FlagReason,
                p.FlaggedAt,
                p.LastModeratorId,
                moderator != null ? $"{moderator.FirstName} {moderator.LastName}" : null,
                p.LastModeratedAt,
                product?.Name,
                store?.Name,
                seller != null ? $"{seller.FirstName} {seller.LastName}" : null,
                seller?.Email.Value,
                p.IsMain,
                p.CreatedAt);
        }).ToList();
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

    private async Task SendRemovalNotificationAsync(ProductImage photo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(photo.ModerationRemovalReason)) return;

        var product = await _productRepository.GetByIdAsync(photo.ProductId, cancellationToken);
        if (product is null || !product.StoreId.HasValue) return;

        var store = await _storeRepository.GetByIdAsync(product.StoreId.Value, cancellationToken);
        if (store is null) return;

        var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
        if (seller is null) return;

        await _notificationService.SendPhotoRemovedAsync(
            photo.Id,
            product.Id,
            product.Name,
            seller.Email.Value,
            photo.ModerationRemovalReason,
            cancellationToken);
    }
}
