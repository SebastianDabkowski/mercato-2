using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating notification use cases.
/// </summary>
public sealed class NotificationCenterService
{
    private readonly INotificationRepository _repository;

    public NotificationCenterService(INotificationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets notifications for a user with pagination and filtering.
    /// </summary>
    public async Task<NotificationListDto> HandleAsync(GetNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _repository.GetByUserIdAsync(
            query.UserId,
            query.IsRead,
            query.Type,
            pageNumber,
            pageSize,
            cancellationToken);

        var unreadCount = await _repository.GetUnreadCountAsync(query.UserId, cancellationToken);

        var dtos = items.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new NotificationListDto(dtos, totalCount, pageNumber, pageSize, totalPages, unreadCount);
    }

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    public async Task<UnreadNotificationCountDto> HandleAsync(GetUnreadNotificationCountQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var count = await _repository.GetUnreadCountAsync(query.UserId, cancellationToken);
        return new UnreadNotificationCountDto(count);
    }

    /// <summary>
    /// Gets a specific notification by ID.
    /// </summary>
    public async Task<NotificationDto?> HandleAsync(GetNotificationByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var notification = await _repository.GetByIdAsync(query.NotificationId, cancellationToken);
        
        // Verify the notification belongs to the user
        if (notification is null || notification.UserId != query.UserId)
        {
            return null;
        }

        return MapToDto(notification);
    }

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    public async Task<MarkNotificationsReadResultDto> HandleAsync(MarkNotificationAsReadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var notification = await _repository.GetByIdAsync(command.NotificationId, cancellationToken);

        if (notification is null)
        {
            return MarkNotificationsReadResultDto.Failed("Notification not found.");
        }

        // Verify ownership
        if (notification.UserId != command.UserId)
        {
            return MarkNotificationsReadResultDto.Failed("Notification not found.");
        }

        if (notification.IsRead)
        {
            return MarkNotificationsReadResultDto.Succeeded(0);
        }

        notification.MarkAsRead();
        _repository.Update(notification);
        await _repository.SaveChangesAsync(cancellationToken);

        return MarkNotificationsReadResultDto.Succeeded(1);
    }

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    public async Task<MarkNotificationsReadResultDto> HandleAsync(MarkAllNotificationsAsReadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var markedCount = await _repository.MarkAllAsReadAsync(command.UserId, cancellationToken);
        return MarkNotificationsReadResultDto.Succeeded(markedCount);
    }

    /// <summary>
    /// Creates a new notification (for internal use by other services).
    /// </summary>
    public async Task<NotificationDto> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? relatedUrl = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification(
            Guid.NewGuid(),
            userId,
            type,
            title,
            message,
            relatedEntityId,
            relatedEntityType,
            relatedUrl);

        await _repository.AddAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return MapToDto(notification);
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id,
        n.UserId,
        n.Type,
        n.Title,
        n.Message,
        n.RelatedEntityId,
        n.RelatedEntityType,
        n.RelatedUrl,
        n.IsRead,
        n.CreatedAt,
        n.ReadAt);
}
