using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a notification.
/// </summary>
public sealed record NotificationViewModel(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? RelatedUrl,
    bool IsRead,
    DateTime CreatedAt,
    string TimeAgo)
{
    /// <summary>
    /// Gets the Bootstrap icon class for the notification type.
    /// </summary>
    public string IconClass => Type switch
    {
        NotificationType.OrderEvent => "bi-bag-check",
        NotificationType.Return => "bi-arrow-return-left",
        NotificationType.Payout => "bi-cash-coin",
        NotificationType.Message => "bi-chat-dots",
        NotificationType.SystemUpdate => "bi-info-circle",
        _ => "bi-bell"
    };

    /// <summary>
    /// Gets the color class for the notification type.
    /// </summary>
    public string ColorClass => Type switch
    {
        NotificationType.OrderEvent => "text-success",
        NotificationType.Return => "text-warning",
        NotificationType.Payout => "text-primary",
        NotificationType.Message => "text-info",
        NotificationType.SystemUpdate => "text-secondary",
        _ => "text-muted"
    };
}

/// <summary>
/// View model for the notification center page.
/// </summary>
public sealed class NotificationCenterViewModel
{
    public IReadOnlyCollection<NotificationViewModel> Notifications { get; set; } = Array.Empty<NotificationViewModel>();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? FilterType { get; set; }
    public bool? FilterIsRead { get; set; }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
