using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of notification data for UI or API layers.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    string? RelatedUrl,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

/// <summary>
/// Result of getting notifications with pagination info.
/// </summary>
public sealed record NotificationListDto(
    IReadOnlyCollection<NotificationDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    int UnreadCount);

/// <summary>
/// Result of getting unread notification count.
/// </summary>
public sealed record UnreadNotificationCountDto(int Count);

/// <summary>
/// Result of marking notifications as read.
/// </summary>
public sealed record MarkNotificationsReadResultDto(bool Success, int MarkedCount, string? ErrorMessage = null)
{
    public static MarkNotificationsReadResultDto Succeeded(int markedCount) => new(true, markedCount);
    public static MarkNotificationsReadResultDto Failed(string errorMessage) => new(false, 0, errorMessage);
}
