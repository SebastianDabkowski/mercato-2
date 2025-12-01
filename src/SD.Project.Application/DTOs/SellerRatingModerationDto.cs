namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a seller rating in moderation context.
/// </summary>
public record SellerRatingModerationDto(
    Guid SellerRatingId,
    Guid OrderId,
    Guid StoreId,
    Guid BuyerId,
    string? StoreName,
    string? BuyerName,
    int Rating,
    string? Comment,
    string ModerationStatus,
    bool IsFlagged,
    string? FlagReason,
    DateTime? FlaggedAt,
    int ReportCount,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ModeratedAt,
    Guid? ModeratedByUserId,
    string? ModeratorName);

/// <summary>
/// DTO for seller rating moderation statistics.
/// </summary>
public record SellerRatingModerationStatsDto(
    int PendingCount,
    int FlaggedCount,
    int ReportedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// Result of a seller rating moderation action.
/// </summary>
public record SellerRatingModerationResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? SellerRatingId = null);

/// <summary>
/// Result of a batch seller rating moderation action.
/// </summary>
public record BatchSellerRatingModerationResultDto(
    bool Success,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string>? Errors = null);

/// <summary>
/// DTO for seller rating moderation audit log entry.
/// </summary>
public record SellerRatingModerationAuditLogDto(
    Guid Id,
    Guid SellerRatingId,
    Guid? ModeratorId,
    string? ModeratorName,
    string Action,
    string PreviousStatus,
    string NewStatus,
    string? Reason,
    string? Notes,
    bool IsAutomated,
    string? AutomatedRuleName,
    DateTime CreatedAt);
