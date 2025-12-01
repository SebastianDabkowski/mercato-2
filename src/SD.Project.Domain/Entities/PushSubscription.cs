namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a web push notification subscription for a user's device.
/// </summary>
public class PushSubscription
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// The push service endpoint URL.
    /// </summary>
    public string Endpoint { get; private set; } = default!;
    
    /// <summary>
    /// The P256DH key used for encryption.
    /// </summary>
    public string P256dh { get; private set; } = default!;
    
    /// <summary>
    /// The authentication secret.
    /// </summary>
    public string Auth { get; private set; } = default!;
    
    /// <summary>
    /// User-friendly device name (e.g., "Chrome on Windows").
    /// </summary>
    public string? DeviceName { get; private set; }
    
    /// <summary>
    /// Whether push notifications are enabled for this subscription.
    /// </summary>
    public bool IsEnabled { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private PushSubscription()
    {
        // EF Core constructor
    }

    public PushSubscription(
        Guid id,
        Guid userId,
        string endpoint,
        string p256dh,
        string auth,
        string? deviceName = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint is required", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(p256dh))
        {
            throw new ArgumentException("P256DH key is required", nameof(p256dh));
        }

        if (string.IsNullOrWhiteSpace(auth))
        {
            throw new ArgumentException("Auth secret is required", nameof(auth));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        UserId = userId;
        Endpoint = endpoint;
        P256dh = p256dh;
        Auth = auth;
        DeviceName = deviceName;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables push notifications for this subscription.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Disables push notifications for this subscription.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Updates the last used timestamp.
    /// </summary>
    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the subscription keys (when browser refreshes the subscription).
    /// </summary>
    public void UpdateKeys(string p256dh, string auth)
    {
        if (string.IsNullOrWhiteSpace(p256dh))
        {
            throw new ArgumentException("P256DH key is required", nameof(p256dh));
        }

        if (string.IsNullOrWhiteSpace(auth))
        {
            throw new ArgumentException("Auth secret is required", nameof(auth));
        }

        P256dh = p256dh;
        Auth = auth;
    }
}
