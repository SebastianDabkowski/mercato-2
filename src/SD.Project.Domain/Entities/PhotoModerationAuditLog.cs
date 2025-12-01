namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a moderation decision record for a product photo for audit and dispute resolution.
/// </summary>
public class PhotoModerationAuditLog
{
    public Guid Id { get; private set; }
    public Guid PhotoId { get; private set; }
    public Guid ModeratorId { get; private set; }
    public PhotoModerationStatus Decision { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PhotoModerationAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new photo moderation audit log entry.
    /// </summary>
    /// <param name="photoId">The ID of the moderated photo.</param>
    /// <param name="moderatorId">The ID of the moderator.</param>
    /// <param name="decision">The moderation decision.</param>
    /// <param name="reason">Optional reason for the decision.</param>
    public PhotoModerationAuditLog(Guid photoId, Guid moderatorId, PhotoModerationStatus decision, string? reason = null)
    {
        if (photoId == Guid.Empty)
        {
            throw new ArgumentException("Photo ID is required.", nameof(photoId));
        }

        if (moderatorId == Guid.Empty)
        {
            throw new ArgumentException("Moderator ID is required.", nameof(moderatorId));
        }

        Id = Guid.NewGuid();
        PhotoId = photoId;
        ModeratorId = moderatorId;
        Decision = decision;
        Reason = reason?.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
