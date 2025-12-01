namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a review in admin moderation context.
/// </summary>
public sealed record AdminReviewModerationViewModel(
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
    string? ModeratorName);

/// <summary>
/// View model for review moderation statistics in dashboard.
/// </summary>
public sealed record ReviewModerationStatsViewModel(
    int PendingCount,
    int FlaggedCount,
    int ReportedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// View model for moderation audit log entry.
/// </summary>
public sealed record ReviewModerationAuditLogViewModel(
    Guid Id,
    Guid ReviewId,
    string? ModeratorName,
    string Action,
    string PreviousStatus,
    string NewStatus,
    string? Reason,
    string? Notes,
    bool IsAutomated,
    string? AutomatedRuleName,
    DateTime CreatedAt);

/// <summary>
/// Helper class for review moderation status display.
/// </summary>
public static class ReviewModerationStatusHelper
{
    /// <summary>
    /// Gets the Bootstrap CSS class for a moderation status badge.
    /// </summary>
    public static string GetStatusBadgeClass(string status) => status switch
    {
        "Pending" => "bg-warning text-dark",
        "Approved" => "bg-success",
        "Rejected" => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets a user-friendly display name for a moderation status.
    /// </summary>
    public static string GetStatusDisplayName(string status) => status switch
    {
        "Pending" => "Pending Review",
        "Approved" => "Approved",
        "Rejected" => "Rejected",
        _ => status
    };

    /// <summary>
    /// Gets the Bootstrap CSS class for a moderation action badge.
    /// </summary>
    public static string GetActionBadgeClass(string action) => action switch
    {
        "Approved" => "bg-success",
        "Rejected" => "bg-danger",
        "Flagged" => "bg-warning text-dark",
        "FlagCleared" => "bg-info",
        "VisibilityChanged" => "bg-secondary",
        "Reported" => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets a user-friendly display name for a moderation action.
    /// </summary>
    public static string GetActionDisplayName(string action) => action switch
    {
        "Approved" => "Approved",
        "Rejected" => "Rejected",
        "Flagged" => "Flagged",
        "FlagCleared" => "Flag Cleared",
        "VisibilityChanged" => "Status Changed",
        "Reported" => "Reported",
        _ => action
    };

    /// <summary>
    /// Gets the star display for a rating.
    /// </summary>
    public static string GetRatingStars(int rating)
    {
        return new string('★', rating) + new string('☆', 5 - rating);
    }
}
