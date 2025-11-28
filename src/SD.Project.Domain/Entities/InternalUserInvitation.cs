namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an invitation token for a pending internal user.
/// The token is sent via email and used to accept the invitation.
/// </summary>
public class InternalUserInvitation
{
    /// <summary>
    /// Unique identifier for the invitation.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the internal user record this invitation is for.
    /// </summary>
    public Guid InternalUserId { get; private set; }

    /// <summary>
    /// The secure invitation token sent via email.
    /// </summary>
    public string Token { get; private set; } = default!;

    /// <summary>
    /// The UTC timestamp when this invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this invitation was accepted, if it has been accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }

    private InternalUserInvitation()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new invitation for an internal user.
    /// </summary>
    /// <param name="internalUserId">The ID of the internal user being invited.</param>
    /// <param name="expirationDays">The number of days until the invitation expires. Default is 7 days.</param>
    public InternalUserInvitation(Guid internalUserId, int expirationDays = 7)
    {
        if (internalUserId == Guid.Empty)
        {
            throw new ArgumentException("Internal user ID is required.", nameof(internalUserId));
        }

        if (expirationDays <= 0)
        {
            throw new ArgumentException("Expiration days must be positive.", nameof(expirationDays));
        }

        Id = Guid.NewGuid();
        InternalUserId = internalUserId;
        Token = GenerateSecureToken();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.AddDays(expirationDays);
        AcceptedAt = null;
    }

    /// <summary>
    /// Indicates whether this invitation is still valid (not expired and not accepted).
    /// </summary>
    public bool IsValid => AcceptedAt is null && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Indicates whether this invitation has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Indicates whether this invitation has been accepted.
    /// </summary>
    public bool IsAccepted => AcceptedAt is not null;

    /// <summary>
    /// Marks the invitation as accepted.
    /// </summary>
    public void Accept()
    {
        if (IsAccepted)
        {
            throw new InvalidOperationException("Invitation has already been accepted.");
        }

        if (IsExpired)
        {
            throw new InvalidOperationException("Invitation has expired.");
        }

        AcceptedAt = DateTime.UtcNow;
    }

    private static string GenerateSecureToken()
    {
        // Generate a cryptographically secure random token (256 bits)
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        // Convert to URL-safe base64
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
