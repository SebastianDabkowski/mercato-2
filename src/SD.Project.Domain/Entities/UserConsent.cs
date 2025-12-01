namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a user's consent decision for a specific consent type.
/// Tracks whether the user has granted or withdrawn consent with timestamps and version info.
/// </summary>
public class UserConsent
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user who made this consent decision.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The ID of the consent type.
    /// </summary>
    public Guid ConsentTypeId { get; private set; }

    /// <summary>
    /// The ID of the consent version that was presented to the user.
    /// </summary>
    public Guid ConsentVersionId { get; private set; }

    /// <summary>
    /// Indicates whether the user has granted consent.
    /// </summary>
    public bool IsGranted { get; private set; }

    /// <summary>
    /// The UTC timestamp when this consent decision was made.
    /// </summary>
    public DateTime ConsentedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when consent was withdrawn (null if still active).
    /// </summary>
    public DateTime? WithdrawnAt { get; private set; }

    /// <summary>
    /// IP address from which the consent was given (for audit purposes).
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string from the browser (for audit purposes).
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// The source of the consent (e.g., "registration", "settings", "checkout").
    /// </summary>
    public string Source { get; private set; } = default!;

    private UserConsent()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new user consent record.
    /// </summary>
    public UserConsent(
        Guid userId,
        Guid consentTypeId,
        Guid consentVersionId,
        bool isGranted,
        string source,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (consentTypeId == Guid.Empty)
        {
            throw new ArgumentException("Consent type ID is required.", nameof(consentTypeId));
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
        UserId = userId;
        ConsentTypeId = consentTypeId;
        ConsentVersionId = consentVersionId;
        IsGranted = isGranted;
        ConsentedAt = DateTime.UtcNow;
        WithdrawnAt = null;
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
        Source = source.Trim();
    }

    /// <summary>
    /// Withdraws the consent.
    /// </summary>
    public void Withdraw()
    {
        if (!IsGranted)
        {
            throw new InvalidOperationException("Consent is not currently granted.");
        }

        if (WithdrawnAt.HasValue)
        {
            throw new InvalidOperationException("Consent has already been withdrawn.");
        }

        IsGranted = false;
        WithdrawnAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Indicates whether the consent is currently active (granted and not withdrawn).
    /// </summary>
    public bool IsActive => IsGranted && !WithdrawnAt.HasValue;
}
