namespace SD.Project.Application.Commands;

/// <summary>
/// Command to mark a single notification as read.
/// </summary>
public sealed record MarkNotificationAsReadCommand(Guid NotificationId, Guid UserId);

/// <summary>
/// Command to mark all notifications as read for a user.
/// </summary>
public sealed record MarkAllNotificationsAsReadCommand(Guid UserId);
