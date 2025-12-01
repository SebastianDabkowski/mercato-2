using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get notifications for a user with optional filtering.
/// </summary>
public sealed record GetNotificationsQuery(
    Guid UserId,
    bool? IsRead = null,
    NotificationType? Type = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get the count of unread notifications for a user.
/// </summary>
public sealed record GetUnreadNotificationCountQuery(Guid UserId);

/// <summary>
/// Query to get a specific notification by ID.
/// </summary>
public sealed record GetNotificationByIdQuery(Guid NotificationId, Guid UserId);
