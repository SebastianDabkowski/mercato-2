namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a review in moderation context.
/// </summary>
public record ReviewModerationDto(
    Guid ReviewId,
    Guid ProductId,
    Guid StoreId,
    Guid BuyerId,
    string? ProductName,
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
/// DTO for review moderation statistics.
/// </summary>
public record ReviewModerationStatsDto(
    int PendingCount,
    int FlaggedCount,
    int ReportedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// Result of a moderation action.
/// </summary>
public record ModerationResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? ReviewId = null);

/// <summary>
/// Result of a batch moderation action.
/// </summary>
public record BatchModerationResultDto(
    bool Success,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string>? Errors = null);

/// <summary>
/// DTO for review moderation audit log entry.
/// </summary>
public record ReviewModerationAuditLogDto(
    Guid Id,
    Guid ReviewId,
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
