using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for review moderation operations.
/// Handles approval, rejection, flagging, and audit logging.
/// </summary>
public sealed class ReviewModerationService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IReviewModerationAuditLogRepository _auditLogRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInternalUserRepository _internalUserRepository;

    public ReviewModerationService(
        IReviewRepository reviewRepository,
        IReviewModerationAuditLogRepository auditLogRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IInternalUserRepository internalUserRepository)
    {
        ArgumentNullException.ThrowIfNull(reviewRepository);
        ArgumentNullException.ThrowIfNull(auditLogRepository);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(internalUserRepository);

        _reviewRepository = reviewRepository;
        _auditLogRepository = auditLogRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _internalUserRepository = internalUserRepository;
    }

    /// <summary>
    /// Gets paginated reviews for moderation.
    /// </summary>
    public async Task<PagedResultDto<ReviewModerationDto>> HandleAsync(
        GetReviewsForModerationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (reviews, totalCount) = await _reviewRepository.GetForModerationPagedAsync(
            query.Status,
            query.IsFlagged,
            query.SearchTerm,
            query.StoreId,
            query.FromDate,
            query.ToDate,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = await MapReviewsToModerationDtosAsync(reviews, cancellationToken);

        return PagedResultDto<ReviewModerationDto>.Create(
            dtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Gets a single review for moderation with full details.
    /// </summary>
    public async Task<ReviewModerationDto?> HandleAsync(
        GetReviewForModerationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var review = await _reviewRepository.GetByIdAsync(query.ReviewId, cancellationToken);
        if (review is null)
        {
            return null;
        }

        var dtos = await MapReviewsToModerationDtosAsync(new[] { review }, cancellationToken);
        return dtos.FirstOrDefault();
    }

    /// <summary>
    /// Gets review moderation statistics.
    /// </summary>
    public async Task<ReviewModerationStatsDto> HandleAsync(
        GetReviewModerationStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var stats = await _reviewRepository.GetModerationStatsAsync(cancellationToken);
        return new ReviewModerationStatsDto(
            stats.PendingCount,
            stats.FlaggedCount,
            stats.ReportedCount,
            stats.ApprovedTodayCount,
            stats.RejectedTodayCount);
    }

    /// <summary>
    /// Gets moderation audit logs for a review.
    /// </summary>
    public async Task<PagedResultDto<ReviewModerationAuditLogDto>> HandleAsync(
        GetReviewModerationAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (logs, totalCount) = await _auditLogRepository.GetPagedAsync(
            query.ReviewId,
            null,
            null,
            null,
            null,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = await MapAuditLogsToDtosAsync(logs, cancellationToken);

        return PagedResultDto<ReviewModerationAuditLogDto>.Create(
            dtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Gets all moderation audit logs with optional filters.
    /// </summary>
    public async Task<PagedResultDto<ReviewModerationAuditLogDto>> HandleAsync(
        GetModerationAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (logs, totalCount) = await _auditLogRepository.GetPagedAsync(
            query.ReviewId,
            query.ModeratorId,
            query.Action,
            query.FromDate,
            query.ToDate,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = await MapAuditLogsToDtosAsync(logs, cancellationToken);

        return PagedResultDto<ReviewModerationAuditLogDto>.Create(
            dtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Approves a review.
    /// </summary>
    public async Task<ModerationResultDto> HandleAsync(
        ApproveReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var review = await _reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
        {
            return new ModerationResultDto(false, "Review not found.");
        }

        var previousStatus = review.ModerationStatus;

        try
        {
            review.ApproveByModerator(command.ModeratorId);
        }
        catch (InvalidOperationException ex)
        {
            return new ModerationResultDto(false, ex.Message);
        }

        _reviewRepository.Update(review);

        // Create audit log
        var auditLog = new ReviewModerationAuditLog(
            review.Id,
            command.ModeratorId,
            ReviewModerationAction.Approved,
            previousStatus,
            ReviewModerationStatus.Approved,
            "Review approved",
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new ModerationResultDto(true, ReviewId: review.Id);
    }

    /// <summary>
    /// Rejects a review.
    /// </summary>
    public async Task<ModerationResultDto> HandleAsync(
        RejectReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new ModerationResultDto(false, "Rejection reason is required.");
        }

        var review = await _reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
        {
            return new ModerationResultDto(false, "Review not found.");
        }

        var previousStatus = review.ModerationStatus;

        try
        {
            review.RejectByModerator(command.ModeratorId, command.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return new ModerationResultDto(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new ModerationResultDto(false, ex.Message);
        }

        _reviewRepository.Update(review);

        // Create audit log
        var auditLog = new ReviewModerationAuditLog(
            review.Id,
            command.ModeratorId,
            ReviewModerationAction.Rejected,
            previousStatus,
            ReviewModerationStatus.Rejected,
            command.Reason,
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new ModerationResultDto(true, ReviewId: review.Id);
    }

    /// <summary>
    /// Flags a review for moderation.
    /// </summary>
    public async Task<ModerationResultDto> HandleAsync(
        FlagReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new ModerationResultDto(false, "Flag reason is required.");
        }

        var review = await _reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
        {
            return new ModerationResultDto(false, "Review not found.");
        }

        var previousStatus = review.ModerationStatus;

        try
        {
            review.Flag(command.Reason);
        }
        catch (ArgumentException ex)
        {
            return new ModerationResultDto(false, ex.Message);
        }

        _reviewRepository.Update(review);

        // Create audit log
        var auditLog = new ReviewModerationAuditLog(
            review.Id,
            command.ModeratorId,
            ReviewModerationAction.Flagged,
            previousStatus,
            review.ModerationStatus,
            command.Reason,
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new ModerationResultDto(true, ReviewId: review.Id);
    }

    /// <summary>
    /// Clears a flag on a review.
    /// </summary>
    public async Task<ModerationResultDto> HandleAsync(
        ClearReviewFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var review = await _reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
        {
            return new ModerationResultDto(false, "Review not found.");
        }

        if (!review.IsFlagged)
        {
            return new ModerationResultDto(false, "Review is not flagged.");
        }

        var previousStatus = review.ModerationStatus;
        review.ClearFlag();
        _reviewRepository.Update(review);

        // Create audit log
        var auditLog = new ReviewModerationAuditLog(
            review.Id,
            command.ModeratorId,
            ReviewModerationAction.FlagCleared,
            previousStatus,
            review.ModerationStatus,
            "Flag cleared",
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new ModerationResultDto(true, ReviewId: review.Id);
    }

    /// <summary>
    /// Resets a review to pending status for re-review.
    /// </summary>
    public async Task<ModerationResultDto> HandleAsync(
        ResetReviewToPendingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new ModerationResultDto(false, "Reason is required.");
        }

        var review = await _reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
        {
            return new ModerationResultDto(false, "Review not found.");
        }

        if (review.ModerationStatus == ReviewModerationStatus.Pending)
        {
            return new ModerationResultDto(false, "Review is already pending.");
        }

        var previousStatus = review.ModerationStatus;
        review.ResetToPending();
        _reviewRepository.Update(review);

        // Create audit log
        var auditLog = new ReviewModerationAuditLog(
            review.Id,
            command.ModeratorId,
            ReviewModerationAction.VisibilityChanged,
            previousStatus,
            ReviewModerationStatus.Pending,
            command.Reason,
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new ModerationResultDto(true, ReviewId: review.Id);
    }

    /// <summary>
    /// Batch approves multiple reviews.
    /// </summary>
    public async Task<BatchModerationResultDto> HandleAsync(
        BatchApproveReviewsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ReviewIds.Count == 0)
        {
            return new BatchModerationResultDto(false, 0, 0, new[] { "No reviews specified." });
        }

        var errors = new List<string>();
        var successCount = 0;

        foreach (var reviewId in command.ReviewIds)
        {
            var result = await HandleAsync(
                new ApproveReviewCommand(reviewId, command.ModeratorId, command.Notes),
                cancellationToken);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                errors.Add($"Review {reviewId}: {result.ErrorMessage}");
            }
        }

        return new BatchModerationResultDto(
            errors.Count == 0,
            successCount,
            errors.Count,
            errors.Count > 0 ? errors.AsReadOnly() : null);
    }

    /// <summary>
    /// Batch rejects multiple reviews.
    /// </summary>
    public async Task<BatchModerationResultDto> HandleAsync(
        BatchRejectReviewsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ReviewIds.Count == 0)
        {
            return new BatchModerationResultDto(false, 0, 0, new[] { "No reviews specified." });
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new BatchModerationResultDto(false, 0, 0, new[] { "Rejection reason is required." });
        }

        var errors = new List<string>();
        var successCount = 0;

        foreach (var reviewId in command.ReviewIds)
        {
            var result = await HandleAsync(
                new RejectReviewCommand(reviewId, command.ModeratorId, command.Reason, command.Notes),
                cancellationToken);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                errors.Add($"Review {reviewId}: {result.ErrorMessage}");
            }
        }

        return new BatchModerationResultDto(
            errors.Count == 0,
            successCount,
            errors.Count,
            errors.Count > 0 ? errors.AsReadOnly() : null);
    }

    private async Task<IReadOnlyList<ReviewModerationDto>> MapReviewsToModerationDtosAsync(
        IEnumerable<Review> reviews,
        CancellationToken cancellationToken)
    {
        var reviewList = reviews.ToList();
        if (reviewList.Count == 0)
        {
            return Array.Empty<ReviewModerationDto>();
        }

        // Get product names
        var productIds = reviewList.Select(r => r.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id, p => p.Name);

        // Get store names
        var storeIds = reviewList.Select(r => r.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id, s => s.Name);

        // Get buyer names
        var buyerIds = reviewList.Select(r => r.BuyerId).Distinct().ToList();
        var buyers = await _userRepository.GetByIdsAsync(buyerIds, cancellationToken);
        var buyerLookup = buyers.ToDictionary(u => u.Id);

        // Get moderator names
        var moderatorIds = reviewList
            .Where(r => r.ModeratedByUserId.HasValue)
            .Select(r => r.ModeratedByUserId!.Value)
            .Distinct()
            .ToList();
        var moderators = moderatorIds.Count > 0
            ? await _internalUserRepository.GetByIdsAsync(moderatorIds, cancellationToken)
            : new List<InternalUser>();
        var moderatorLookup = moderators.ToDictionary(m => m.Id);

        return reviewList.Select(r =>
        {
            productLookup.TryGetValue(r.ProductId, out var productName);
            storeLookup.TryGetValue(r.StoreId, out var storeName);

            string? buyerName = null;
            if (buyerLookup.TryGetValue(r.BuyerId, out var buyer))
            {
                buyerName = !string.IsNullOrEmpty(buyer.FirstName) && !string.IsNullOrEmpty(buyer.LastName)
                    ? $"{buyer.FirstName} {buyer.LastName}"
                    : buyer.Email.Value;
            }

            string? moderatorName = null;
            if (r.ModeratedByUserId.HasValue && moderatorLookup.TryGetValue(r.ModeratedByUserId.Value, out var moderator))
            {
                moderatorName = moderator.Email.Value;
            }

            return new ReviewModerationDto(
                r.Id,
                r.ProductId,
                r.StoreId,
                r.BuyerId,
                productName,
                storeName,
                buyerName,
                r.Rating,
                r.Comment,
                r.ModerationStatus.ToString(),
                r.IsFlagged,
                r.FlagReason,
                r.FlaggedAt,
                r.ReportCount,
                r.RejectionReason,
                r.CreatedAt,
                r.UpdatedAt,
                r.ModeratedAt,
                r.ModeratedByUserId,
                moderatorName);
        }).ToList().AsReadOnly();
    }

    private async Task<IReadOnlyList<ReviewModerationAuditLogDto>> MapAuditLogsToDtosAsync(
        IReadOnlyList<ReviewModerationAuditLog> logs,
        CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
        {
            return Array.Empty<ReviewModerationAuditLogDto>();
        }

        // Get moderator names
        var moderatorIds = logs
            .Where(l => l.ModeratorId.HasValue)
            .Select(l => l.ModeratorId!.Value)
            .Distinct()
            .ToList();
        var moderators = moderatorIds.Count > 0
            ? await _internalUserRepository.GetByIdsAsync(moderatorIds, cancellationToken)
            : new List<InternalUser>();
        var moderatorLookup = moderators.ToDictionary(m => m.Id);

        return logs.Select(l =>
        {
            string? moderatorName = null;
            if (l.ModeratorId.HasValue && moderatorLookup.TryGetValue(l.ModeratorId.Value, out var moderator))
            {
                moderatorName = moderator.Email.Value;
            }

            return new ReviewModerationAuditLogDto(
                l.Id,
                l.ReviewId,
                l.ModeratorId,
                moderatorName,
                l.Action.ToString(),
                l.PreviousStatus.ToString(),
                l.NewStatus.ToString(),
                l.Reason,
                l.Notes,
                l.IsAutomated,
                l.AutomatedRuleName,
                l.CreatedAt);
        }).ToList().AsReadOnly();
    }
}
