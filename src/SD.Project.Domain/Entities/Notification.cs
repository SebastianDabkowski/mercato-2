namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a user notification.
/// </summary>
public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public string? RelatedUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification()
    {
        // EF Core constructor
    }

    public Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? relatedUrl = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required", nameof(message));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        RelatedUrl = relatedUrl;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the notification as read.
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks the notification as unread.
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
    }

    /// <summary>
    /// Creates a notification with a specified creation time (for seeding/testing purposes).
    /// </summary>
    public static Notification CreateWithTimestamp(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string message,
        DateTime createdAt,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? relatedUrl = null)
    {
        var notification = new Notification(id, userId, type, title, message, relatedEntityId, relatedEntityType, relatedUrl);
        notification.CreatedAt = createdAt;
        return notification;
    }
}
