namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a unique, time-limited email verification token for user accounts.
/// </summary>
public class EmailVerificationToken
{
    /// <summary>
    /// The unique identifier of the token.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user this token is for.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The unique token string used in the verification link.
    /// </summary>
    public string Token { get; private set; } = default!;

    /// <summary>
    /// The UTC timestamp when this token expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this token was used, if it has been used.
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this token was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private EmailVerificationToken()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new email verification token for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to create the token for.</param>
    /// <param name="validityPeriod">The time period the token will be valid for. Defaults to 24 hours.</param>
    public EmailVerificationToken(Guid userId, TimeSpan? validityPeriod = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var validity = validityPeriod ?? TimeSpan.FromHours(24);
        if (validity <= TimeSpan.Zero)
        {
            throw new ArgumentException("Validity period must be positive.", nameof(validityPeriod));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Token = GenerateSecureToken();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(validity);
        UsedAt = null;
    }

    /// <summary>
    /// Indicates whether this token is still valid and can be used.
    /// </summary>
    public bool IsValid => UsedAt is null && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Indicates whether this token has already been used.
    /// </summary>
    public bool IsUsed => UsedAt is not null;

    /// <summary>
    /// Indicates whether this token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Marks this token as used. Can only be called once on a valid token.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the token has already been used or has expired.</exception>
    public void MarkAsUsed()
    {
        if (IsUsed)
        {
            throw new InvalidOperationException("This verification token has already been used.");
        }

        if (IsExpired)
        {
            throw new InvalidOperationException("This verification token has expired.");
        }

        UsedAt = DateTime.UtcNow;
    }

    private static string GenerateSecureToken()
    {
        // Generate a cryptographically secure random token
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
