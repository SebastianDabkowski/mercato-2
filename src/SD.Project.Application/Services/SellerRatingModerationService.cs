using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for seller rating moderation operations.
/// Handles approval, rejection, flagging, and audit logging.
/// </summary>
public sealed class SellerRatingModerationService
{
    private readonly ISellerRatingRepository _sellerRatingRepository;
    private readonly ISellerRatingModerationAuditLogRepository _auditLogRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInternalUserRepository _internalUserRepository;

    public SellerRatingModerationService(
        ISellerRatingRepository sellerRatingRepository,
        ISellerRatingModerationAuditLogRepository auditLogRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IInternalUserRepository internalUserRepository)
    {
        ArgumentNullException.ThrowIfNull(sellerRatingRepository);
        ArgumentNullException.ThrowIfNull(auditLogRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(internalUserRepository);

        _sellerRatingRepository = sellerRatingRepository;
        _auditLogRepository = auditLogRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _internalUserRepository = internalUserRepository;
    }

    /// <summary>
    /// Gets paginated seller ratings for moderation.
    /// </summary>
    public async Task<PagedResultDto<SellerRatingModerationDto>> HandleAsync(
        GetSellerRatingsForModerationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (ratings, totalCount) = await _sellerRatingRepository.GetForModerationPagedAsync(
            query.Status,
            query.IsFlagged,
            query.SearchTerm,
            query.StoreId,
            query.FromDate,
            query.ToDate,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = await MapRatingsToModerationDtosAsync(ratings, cancellationToken);

        return PagedResultDto<SellerRatingModerationDto>.Create(
            dtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Gets a single seller rating for moderation with full details.
    /// </summary>
    public async Task<SellerRatingModerationDto?> HandleAsync(
        GetSellerRatingForModerationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rating = await _sellerRatingRepository.GetByIdAsync(query.SellerRatingId, cancellationToken);
        if (rating is null)
        {
            return null;
        }

        var dtos = await MapRatingsToModerationDtosAsync(new[] { rating }, cancellationToken);
        return dtos.FirstOrDefault();
    }

    /// <summary>
    /// Gets seller rating moderation statistics.
    /// </summary>
    public async Task<SellerRatingModerationStatsDto> HandleAsync(
        GetSellerRatingModerationStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var stats = await _sellerRatingRepository.GetModerationStatsAsync(cancellationToken);
        return new SellerRatingModerationStatsDto(
            stats.PendingCount,
            stats.FlaggedCount,
            stats.ReportedCount,
            stats.ApprovedTodayCount,
            stats.RejectedTodayCount);
    }

    /// <summary>
    /// Gets moderation audit logs for a seller rating.
    /// </summary>
    public async Task<PagedResultDto<SellerRatingModerationAuditLogDto>> HandleAsync(
        GetSellerRatingModerationAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (logs, totalCount) = await _auditLogRepository.GetPagedAsync(
            query.SellerRatingId,
            null,
            null,
            null,
            null,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = await MapAuditLogsToDtosAsync(logs, cancellationToken);

        return PagedResultDto<SellerRatingModerationAuditLogDto>.Create(
            dtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Gets all moderation audit logs with optional filters.
    /// </summary>
    public async Task<PagedResultDto<SellerRatingModerationAuditLogDto>> HandleAsync(
        GetSellerRatingModerationAuditLogsAllQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (logs, totalCount) = await _auditLogRepository.GetPagedAsync(
            query.SellerRatingId,
            query.ModeratorId,
            query.Action,
            query.FromDate,
            query.ToDate,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = await MapAuditLogsToDtosAsync(logs, cancellationToken);

        return PagedResultDto<SellerRatingModerationAuditLogDto>.Create(
            dtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Approves a seller rating.
    /// </summary>
    public async Task<SellerRatingModerationResultDto> HandleAsync(
        ApproveSellerRatingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rating = await _sellerRatingRepository.GetByIdAsync(command.SellerRatingId, cancellationToken);
        if (rating is null)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating not found.");
        }

        var previousStatus = rating.ModerationStatus;

        try
        {
            rating.ApproveByModerator(command.ModeratorId);
        }
        catch (InvalidOperationException ex)
        {
            return new SellerRatingModerationResultDto(false, ex.Message);
        }

        _sellerRatingRepository.Update(rating);

        // Create audit log
        var auditLog = new SellerRatingModerationAuditLog(
            rating.Id,
            command.ModeratorId,
            SellerRatingModerationAction.Approved,
            previousStatus,
            SellerRatingModerationStatus.Approved,
            "Seller rating approved",
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _sellerRatingRepository.SaveChangesAsync(cancellationToken);

        return new SellerRatingModerationResultDto(true, SellerRatingId: rating.Id);
    }

    /// <summary>
    /// Rejects a seller rating.
    /// </summary>
    public async Task<SellerRatingModerationResultDto> HandleAsync(
        RejectSellerRatingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new SellerRatingModerationResultDto(false, "Rejection reason is required.");
        }

        var rating = await _sellerRatingRepository.GetByIdAsync(command.SellerRatingId, cancellationToken);
        if (rating is null)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating not found.");
        }

        var previousStatus = rating.ModerationStatus;

        try
        {
            rating.RejectByModerator(command.ModeratorId, command.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return new SellerRatingModerationResultDto(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new SellerRatingModerationResultDto(false, ex.Message);
        }

        _sellerRatingRepository.Update(rating);

        // Create audit log
        var auditLog = new SellerRatingModerationAuditLog(
            rating.Id,
            command.ModeratorId,
            SellerRatingModerationAction.Rejected,
            previousStatus,
            SellerRatingModerationStatus.Rejected,
            command.Reason,
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _sellerRatingRepository.SaveChangesAsync(cancellationToken);

        return new SellerRatingModerationResultDto(true, SellerRatingId: rating.Id);
    }

    /// <summary>
    /// Flags a seller rating for moderation.
    /// </summary>
    public async Task<SellerRatingModerationResultDto> HandleAsync(
        FlagSellerRatingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new SellerRatingModerationResultDto(false, "Flag reason is required.");
        }

        var rating = await _sellerRatingRepository.GetByIdAsync(command.SellerRatingId, cancellationToken);
        if (rating is null)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating not found.");
        }

        var previousStatus = rating.ModerationStatus;

        try
        {
            rating.Flag(command.Reason);
        }
        catch (ArgumentException ex)
        {
            return new SellerRatingModerationResultDto(false, ex.Message);
        }

        _sellerRatingRepository.Update(rating);

        // Create audit log
        var auditLog = new SellerRatingModerationAuditLog(
            rating.Id,
            command.ModeratorId,
            SellerRatingModerationAction.Flagged,
            previousStatus,
            rating.ModerationStatus,
            command.Reason,
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _sellerRatingRepository.SaveChangesAsync(cancellationToken);

        return new SellerRatingModerationResultDto(true, SellerRatingId: rating.Id);
    }

    /// <summary>
    /// Clears a flag on a seller rating.
    /// </summary>
    public async Task<SellerRatingModerationResultDto> HandleAsync(
        ClearSellerRatingFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rating = await _sellerRatingRepository.GetByIdAsync(command.SellerRatingId, cancellationToken);
        if (rating is null)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating not found.");
        }

        if (!rating.IsFlagged)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating is not flagged.");
        }

        var previousStatus = rating.ModerationStatus;
        rating.ClearFlag();
        _sellerRatingRepository.Update(rating);

        // Create audit log
        var auditLog = new SellerRatingModerationAuditLog(
            rating.Id,
            command.ModeratorId,
            SellerRatingModerationAction.FlagCleared,
            previousStatus,
            rating.ModerationStatus,
            "Flag cleared",
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _sellerRatingRepository.SaveChangesAsync(cancellationToken);

        return new SellerRatingModerationResultDto(true, SellerRatingId: rating.Id);
    }

    /// <summary>
    /// Resets a seller rating to pending status for re-review.
    /// </summary>
    public async Task<SellerRatingModerationResultDto> HandleAsync(
        ResetSellerRatingToPendingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new SellerRatingModerationResultDto(false, "Reason is required.");
        }

        var rating = await _sellerRatingRepository.GetByIdAsync(command.SellerRatingId, cancellationToken);
        if (rating is null)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating not found.");
        }

        if (rating.ModerationStatus == SellerRatingModerationStatus.Pending)
        {
            return new SellerRatingModerationResultDto(false, "Seller rating is already pending.");
        }

        var previousStatus = rating.ModerationStatus;
        rating.ResetToPending();
        _sellerRatingRepository.Update(rating);

        // Create audit log
        var auditLog = new SellerRatingModerationAuditLog(
            rating.Id,
            command.ModeratorId,
            SellerRatingModerationAction.VisibilityChanged,
            previousStatus,
            SellerRatingModerationStatus.Pending,
            command.Reason,
            command.Notes);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _sellerRatingRepository.SaveChangesAsync(cancellationToken);

        return new SellerRatingModerationResultDto(true, SellerRatingId: rating.Id);
    }

    /// <summary>
    /// Batch approves multiple seller ratings.
    /// </summary>
    public async Task<BatchSellerRatingModerationResultDto> HandleAsync(
        BatchApproveSellerRatingsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.SellerRatingIds.Count == 0)
        {
            return new BatchSellerRatingModerationResultDto(false, 0, 0, new[] { "No seller ratings specified." });
        }

        var errors = new List<string>();
        var successCount = 0;

        foreach (var ratingId in command.SellerRatingIds)
        {
            var result = await HandleAsync(
                new ApproveSellerRatingCommand(ratingId, command.ModeratorId, command.Notes),
                cancellationToken);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                errors.Add($"Seller rating {ratingId}: {result.ErrorMessage}");
            }
        }

        return new BatchSellerRatingModerationResultDto(
            errors.Count == 0,
            successCount,
            errors.Count,
            errors.Count > 0 ? errors.AsReadOnly() : null);
    }

    /// <summary>
    /// Batch rejects multiple seller ratings.
    /// </summary>
    public async Task<BatchSellerRatingModerationResultDto> HandleAsync(
        BatchRejectSellerRatingsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.SellerRatingIds.Count == 0)
        {
            return new BatchSellerRatingModerationResultDto(false, 0, 0, new[] { "No seller ratings specified." });
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new BatchSellerRatingModerationResultDto(false, 0, 0, new[] { "Rejection reason is required." });
        }

        var errors = new List<string>();
        var successCount = 0;

        foreach (var ratingId in command.SellerRatingIds)
        {
            var result = await HandleAsync(
                new RejectSellerRatingCommand(ratingId, command.ModeratorId, command.Reason, command.Notes),
                cancellationToken);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                errors.Add($"Seller rating {ratingId}: {result.ErrorMessage}");
            }
        }

        return new BatchSellerRatingModerationResultDto(
            errors.Count == 0,
            successCount,
            errors.Count,
            errors.Count > 0 ? errors.AsReadOnly() : null);
    }

    private async Task<IReadOnlyList<SellerRatingModerationDto>> MapRatingsToModerationDtosAsync(
        IEnumerable<SellerRating> ratings,
        CancellationToken cancellationToken)
    {
        var ratingList = ratings.ToList();
        if (ratingList.Count == 0)
        {
            return Array.Empty<SellerRatingModerationDto>();
        }

        // Get store names
        var storeIds = ratingList.Select(r => r.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id, s => s.Name);

        // Get buyer names
        var buyerIds = ratingList.Select(r => r.BuyerId).Distinct().ToList();
        var buyers = await _userRepository.GetByIdsAsync(buyerIds, cancellationToken);
        var buyerLookup = buyers.ToDictionary(u => u.Id);

        // Get moderator names
        var moderatorIds = ratingList
            .Where(r => r.ModeratedByUserId.HasValue)
            .Select(r => r.ModeratedByUserId!.Value)
            .Distinct()
            .ToList();
        var moderators = moderatorIds.Count > 0
            ? await _internalUserRepository.GetByIdsAsync(moderatorIds, cancellationToken)
            : new List<InternalUser>();
        var moderatorLookup = moderators.ToDictionary(m => m.Id);

        return ratingList.Select(r =>
        {
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

            return new SellerRatingModerationDto(
                r.Id,
                r.OrderId,
                r.StoreId,
                r.BuyerId,
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

    private async Task<IReadOnlyList<SellerRatingModerationAuditLogDto>> MapAuditLogsToDtosAsync(
        IReadOnlyList<SellerRatingModerationAuditLog> logs,
        CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
        {
            return Array.Empty<SellerRatingModerationAuditLogDto>();
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

            return new SellerRatingModerationAuditLogDto(
                l.Id,
                l.SellerRatingId,
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
