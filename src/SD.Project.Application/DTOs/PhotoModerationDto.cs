using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a photo in the moderation queue.
/// </summary>
public record PhotoModerationDto(
    Guid PhotoId,
    Guid ProductId,
    Guid? StoreId,
    string FileName,
    string ImageUrl,
    string ThumbnailUrl,
    PhotoModerationStatus ModerationStatus,
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
/// DTO for photo moderation statistics.
/// </summary>
public record PhotoModerationStatsDto(
    int PendingCount,
    int FlaggedCount,
    int ApprovedCount,
    int RemovedCount,
    int ApprovedTodayCount,
    int RemovedTodayCount);

/// <summary>
/// Result of a photo moderation action.
/// </summary>
public record PhotoModerationResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? PhotoId = null)
{
    public static PhotoModerationResultDto Succeeded(Guid photoId) => new(true, null, photoId);
    public static PhotoModerationResultDto Failed(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Result of a batch photo moderation action.
/// </summary>
public record BatchPhotoModerationResultDto(
    bool Success,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string>? Errors = null)
{
    public static BatchPhotoModerationResultDto Succeeded(int successCount) =>
        new(true, successCount, 0);

    public static BatchPhotoModerationResultDto PartialSuccess(int successCount, int failureCount, IReadOnlyList<string> errors) =>
        new(successCount > 0, successCount, failureCount, errors);

    public static BatchPhotoModerationResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, 0, errors.Count, errors);
}

/// <summary>
/// DTO for photo moderation audit log entry.
/// </summary>
public record PhotoModerationAuditLogDto(
    Guid Id,
    Guid PhotoId,
    Guid ModeratorId,
    string? ModeratorName,
    PhotoModerationStatus Decision,
    string? Reason,
    DateTime CreatedAt);
