namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an audit log entry for user consent changes.
/// Used to retain previous versions for audit purposes when consent changes.
/// </summary>
public class UserConsentAuditLog
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user consent record that was modified.
    /// </summary>
    public Guid UserConsentId { get; private set; }

    /// <summary>
    /// The ID of the user who made the consent change.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The type of action performed.
    /// </summary>
    public UserConsentAuditAction Action { get; private set; }

    /// <summary>
    /// The consent version ID at the time of this action.
    /// </summary>
    public Guid ConsentVersionId { get; private set; }

    /// <summary>
    /// IP address from which the change was made.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string from the browser.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// The source of the consent change (e.g., "registration", "settings").
    /// </summary>
    public string Source { get; private set; } = default!;

    /// <summary>
    /// The UTC timestamp when this action was performed.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private UserConsentAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public UserConsentAuditLog(
        Guid userConsentId,
        Guid userId,
        UserConsentAuditAction action,
        Guid consentVersionId,
        string source,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userConsentId == Guid.Empty)
        {
            throw new ArgumentException("User consent ID is required.", nameof(userConsentId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (consentVersionId == Guid.Empty)
        {
            throw new ArgumentException("Consent version ID is required.", nameof(consentVersionId));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        Id = Guid.NewGuid();
        UserConsentId = userConsentId;
        UserId = userId;
        Action = action;
        ConsentVersionId = consentVersionId;
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
        Source = source.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Defines the types of actions that can be audited for user consent changes.
/// </summary>
public enum UserConsentAuditAction
{
    /// <summary>
    /// Consent was granted.
    /// </summary>
    Granted = 0,

    /// <summary>
    /// Consent was withdrawn.
    /// </summary>
    Withdrawn = 1,

    /// <summary>
    /// Consent was renewed (granted again after withdrawal or version update).
    /// </summary>
    Renewed = 2
}
