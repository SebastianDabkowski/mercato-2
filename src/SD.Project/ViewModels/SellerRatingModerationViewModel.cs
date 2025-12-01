namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a seller rating in admin moderation context.
/// </summary>
public sealed record AdminSellerRatingModerationViewModel(
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
    string? ModeratorName);

/// <summary>
/// View model for seller rating moderation statistics in dashboard.
/// </summary>
public sealed record SellerRatingModerationStatsViewModel(
    int PendingCount,
    int FlaggedCount,
    int ReportedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// View model for seller rating moderation audit log entry.
/// </summary>
public sealed record SellerRatingModerationAuditLogViewModel(
    Guid Id,
    Guid SellerRatingId,
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
/// Helper class for seller rating moderation status display.
/// </summary>
public static class SellerRatingModerationStatusHelper
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
        var clampedRating = Math.Clamp(rating, 0, 5);
        return new string('★', clampedRating) + new string('☆', 5 - clampedRating);
    }
}
