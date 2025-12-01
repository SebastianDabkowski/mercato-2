using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a photo in the moderation queue.
/// </summary>
public record PhotoModerationViewModel(
    Guid PhotoId,
    Guid ProductId,
    Guid? StoreId,
    string FileName,
    string ImageUrl,
    string ThumbnailUrl,
    string ModerationStatus,
    string? ModerationRemovalReason,
    bool IsFlagged,
    string? FlagReason,
    DateTime? FlaggedAt,
    Guid? LastModeratorId,
    string? LastModeratorName,
    DateTime? LastModeratedAt,
    string? ProductName,
    string? StoreName,
    string? SellerName,
    string? SellerEmail,
    bool IsMain,
    DateTime CreatedAt);

/// <summary>
/// View model for photo moderation statistics.
/// </summary>
public record PhotoModerationStatsViewModel(
    int PendingCount,
    int FlaggedCount,
    int ApprovedCount,
    int RemovedCount,
    int ApprovedTodayCount,
    int RemovedTodayCount);

/// <summary>
/// View model for photo moderation audit log entry.
/// </summary>
public record PhotoModerationAuditLogViewModel(
    Guid Id,
    Guid PhotoId,
    Guid ModeratorId,
    string? ModeratorName,
    string Decision,
    string? Reason,
    DateTime CreatedAt);

/// <summary>
/// Helper class for photo moderation status display.
/// </summary>
public static class PhotoModerationStatusHelper
{
    public static string GetStatusDisplayName(string status)
    {
        return status switch
        {
            "PendingReview" => "Pending Review",
            "Approved" => "Approved",
            "Removed" => "Removed",
            _ => status
        };
    }

    public static string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "PendingReview" => "bg-warning text-dark",
            "Approved" => "bg-success",
            "Removed" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public static string GetStatusDisplayName(PhotoModerationStatus status)
    {
        return status switch
        {
            PhotoModerationStatus.PendingReview => "Pending Review",
            PhotoModerationStatus.Approved => "Approved",
            PhotoModerationStatus.Removed => "Removed",
            _ => status.ToString()
        };
    }

    public static string GetStatusBadgeClass(PhotoModerationStatus status)
    {
        return status switch
        {
            PhotoModerationStatus.PendingReview => "bg-warning text-dark",
            PhotoModerationStatus.Approved => "bg-success",
            PhotoModerationStatus.Removed => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
