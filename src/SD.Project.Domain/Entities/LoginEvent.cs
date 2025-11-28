namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a login event for auditing purposes.
/// Records both successful and failed login attempts for security analysis.
/// </summary>
public class LoginEvent
{
    /// <summary>
    /// The unique identifier of the login event.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user this login event is associated with.
    /// Null if the user was not found (failed login with invalid email).
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// The email address used in the login attempt.
    /// </summary>
    public string Email { get; private set; } = default!;

    /// <summary>
    /// Indicates whether the login attempt was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// The type of login event (e.g., Password, Social, TwoFactor).
    /// </summary>
    public LoginEventType EventType { get; private set; }

    /// <summary>
    /// Additional details about the login failure, if applicable.
    /// Should not contain sensitive information.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// The IP address from which the login attempt originated.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// The user agent string of the client making the login attempt.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Geographic location derived from IP address, if available.
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// The UTC timestamp when the login event occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Indicates if this login event triggered a security alert.
    /// </summary>
    public bool AlertTriggered { get; private set; }

    /// <summary>
    /// The UTC timestamp when this record should be deleted for retention compliance.
    /// </summary>
    public DateTime RetentionExpiresAt { get; private set; }

    private LoginEvent()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new login event record.
    /// </summary>
    /// <param name="userId">The user ID (null if user not found).</param>
    /// <param name="email">The email used in the login attempt.</param>
    /// <param name="isSuccess">Whether the login was successful.</param>
    /// <param name="eventType">The type of login event.</param>
    /// <param name="failureReason">Reason for failure (null if successful).</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    /// <param name="location">Geographic location if available.</param>
    /// <param name="retentionDays">Number of days to retain this record (default 90 days).</param>
    public LoginEvent(
        Guid? userId,
        string email,
        bool isSuccess,
        LoginEventType eventType,
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? location = null,
        int retentionDays = 90)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (retentionDays < 1)
        {
            throw new ArgumentException("Retention days must be at least 1.", nameof(retentionDays));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Email = email.Trim().ToLowerInvariant();
        IsSuccess = isSuccess;
        EventType = eventType;
        FailureReason = failureReason;
        IpAddress = ipAddress?.Length > 45 ? ipAddress[..45] : ipAddress; // IPv6 max length
        UserAgent = userAgent?.Length > 512 ? userAgent[..512] : userAgent;
        Location = location?.Length > 255 ? location[..255] : location;
        OccurredAt = DateTime.UtcNow;
        AlertTriggered = false;
        RetentionExpiresAt = OccurredAt.AddDays(retentionDays);
    }

    /// <summary>
    /// Marks this login event as having triggered a security alert.
    /// </summary>
    public void MarkAlertTriggered()
    {
        AlertTriggered = true;
    }
}
