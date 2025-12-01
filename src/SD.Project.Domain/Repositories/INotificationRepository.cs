using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for notification persistence operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets a notification by ID.
    /// </summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a user with optional filtering and pagination.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="isReadFilter">Optional filter by read status.</param>
    /// <param name="type">Optional filter by notification type.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (notifications, total count).</returns>
    Task<(IReadOnlyCollection<Notification> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        bool? isReadFilter = null,
        NotificationType? type = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification.
    /// </summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a notification.
    /// </summary>
    void Update(Notification notification);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of notifications marked as read.</returns>
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
