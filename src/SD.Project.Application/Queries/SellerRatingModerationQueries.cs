using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get seller ratings for moderation with filtering and pagination.
/// </summary>
public record GetSellerRatingsForModerationQuery(
    SellerRatingModerationStatus? Status = null,
    bool? IsFlagged = null,
    string? SearchTerm = null,
    Guid? StoreId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get a single seller rating for moderation with full details.
/// </summary>
public record GetSellerRatingForModerationQuery(Guid SellerRatingId);

/// <summary>
/// Query to get seller rating moderation statistics.
/// </summary>
public record GetSellerRatingModerationStatsQuery();

/// <summary>
/// Query to get moderation audit logs for a seller rating.
/// </summary>
public record GetSellerRatingModerationAuditLogsQuery(
    Guid SellerRatingId,
    int PageNumber = 1,
    int PageSize = 50);

/// <summary>
/// Query to get all seller rating moderation audit logs with optional filters.
/// </summary>
public record GetSellerRatingModerationAuditLogsAllQuery(
    Guid? SellerRatingId = null,
    Guid? ModeratorId = null,
    SellerRatingModerationAction? Action = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50);
