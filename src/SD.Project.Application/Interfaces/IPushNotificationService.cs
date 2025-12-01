namespace SD.Project.Application.Interfaces;

/// <summary>
/// Push notification payload.
/// </summary>
public sealed record PushNotificationPayload(
    string Title,
    string Body,
    string? Icon = null,
    string? Badge = null,
    string? Url = null,
    string? Tag = null,
    Dictionary<string, string>? Data = null);

/// <summary>
/// Abstraction for sending web push notifications to users.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Gets the VAPID public key for client-side subscription.
    /// </summary>
    string GetVapidPublicKey();

    /// <summary>
    /// Sends a push notification to all active subscriptions for a user.
    /// </summary>
    /// <param name="userId">The user to send the notification to.</param>
    /// <param name="payload">The notification content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of successful deliveries.</returns>
    Task<int> SendToUserAsync(Guid userId, PushNotificationPayload payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to a specific subscription endpoint.
    /// </summary>
    /// <param name="endpoint">The push service endpoint.</param>
    /// <param name="p256dh">The P256DH key.</param>
    /// <param name="auth">The auth secret.</param>
    /// <param name="payload">The notification content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the notification was sent successfully.</returns>
    Task<bool> SendToEndpointAsync(
        string endpoint,
        string p256dh,
        string auth,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default);
}
