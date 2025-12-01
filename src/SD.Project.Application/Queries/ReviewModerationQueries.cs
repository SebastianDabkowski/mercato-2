using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get reviews for moderation with filtering and pagination.
/// </summary>
public record GetReviewsForModerationQuery(
    ReviewModerationStatus? Status = null,
    bool? IsFlagged = null,
    string? SearchTerm = null,
    Guid? StoreId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get a single review for moderation with full details.
/// </summary>
public record GetReviewForModerationQuery(Guid ReviewId);

/// <summary>
/// Query to get review moderation statistics.
/// </summary>
public record GetReviewModerationStatsQuery();

/// <summary>
/// Query to get moderation audit logs for a review.
/// </summary>
public record GetReviewModerationAuditLogsQuery(
    Guid ReviewId,
    int PageNumber = 1,
    int PageSize = 50);

/// <summary>
/// Query to get all moderation audit logs with optional filters.
/// </summary>
public record GetModerationAuditLogsQuery(
    Guid? ReviewId = null,
    Guid? ModeratorId = null,
    ReviewModerationAction? Action = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50);
