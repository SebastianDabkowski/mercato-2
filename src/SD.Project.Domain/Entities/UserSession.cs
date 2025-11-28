namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an active user session with a secure token.
/// Sessions can be invalidated from the backend to support logout,
/// security revocation, and horizontal scaling.
/// </summary>
public class UserSession
{
    /// <summary>
    /// The unique identifier of the session.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user this session belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The secure session token used to validate requests.
    /// This token is stored in the authentication cookie and validated against the database.
    /// </summary>
    public string Token { get; private set; } = default!;

    /// <summary>
    /// The UTC timestamp when this session was created (login time).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this session expires (absolute expiration).
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// The UTC timestamp of the last activity on this session.
    /// Used for sliding expiration.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this session was revoked, if it has been revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// The user agent string of the client that created this session.
    /// Useful for session management UI and security auditing.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// The IP address of the client that created this session.
    /// Useful for security auditing.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Indicates whether this is a persistent session (Remember Me).
    /// </summary>
    public bool IsPersistent { get; private set; }

    private UserSession()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new user session with a secure token.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="absoluteExpiration">The absolute expiration time for the session.</param>
    /// <param name="isPersistent">Whether this is a persistent session.</param>
    /// <param name="userAgent">The user agent of the client.</param>
    /// <param name="ipAddress">The IP address of the client.</param>
    public UserSession(
        Guid userId,
        TimeSpan absoluteExpiration,
        bool isPersistent,
        string? userAgent = null,
        string? ipAddress = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (absoluteExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Expiration must be positive.", nameof(absoluteExpiration));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Token = GenerateSecureToken();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(absoluteExpiration);
        LastActivityAt = CreatedAt;
        RevokedAt = null;
        UserAgent = userAgent?.Length > 512 ? userAgent[..512] : userAgent;
        IpAddress = ipAddress?.Length > 45 ? ipAddress[..45] : ipAddress; // IPv6 max length
        IsPersistent = isPersistent;
    }

    /// <summary>
    /// Indicates whether this session is still valid (not expired and not revoked).
    /// </summary>
    public bool IsValid => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Indicates whether this session has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Indicates whether this session has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Updates the last activity timestamp.
    /// Used for sliding expiration calculation.
    /// </summary>
    public void UpdateActivity()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot update activity on an invalid session.");
        }

        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes this session, making it invalid for future requests.
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
        {
            return; // Already revoked, no-op
        }

        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Extends the session expiration time.
    /// Used for sliding expiration.
    /// </summary>
    /// <param name="newExpiration">The new expiration time from now.</param>
    public void ExtendExpiration(TimeSpan newExpiration)
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot extend an invalid session.");
        }

        if (newExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Expiration must be positive.", nameof(newExpiration));
        }

        ExpiresAt = DateTime.UtcNow.Add(newExpiration);
        LastActivityAt = DateTime.UtcNow;
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
