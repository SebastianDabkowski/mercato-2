namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of a push subscription for UI or API layers.
/// </summary>
public sealed record PushSubscriptionDto(
    Guid Id,
    Guid UserId,
    string Endpoint,
    string? DeviceName,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? LastUsedAt);

/// <summary>
/// Result of subscribing to push notifications.
/// </summary>
public sealed record PushSubscriptionResultDto(bool Success, Guid? SubscriptionId = null, string? ErrorMessage = null)
{
    public static PushSubscriptionResultDto Succeeded(Guid subscriptionId) => new(true, subscriptionId);
    public static PushSubscriptionResultDto Failed(string errorMessage) => new(false, null, errorMessage);
}

/// <summary>
/// Result of unsubscribing from push notifications.
/// </summary>
public sealed record PushUnsubscribeResultDto(bool Success, string? ErrorMessage = null)
{
    public static PushUnsubscribeResultDto Succeeded() => new(true);
    public static PushUnsubscribeResultDto Failed(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Configuration for VAPID keys (public key for clients).
/// </summary>
public sealed record VapidPublicKeyDto(string PublicKey);

/// <summary>
/// Result of sending a push notification.
/// </summary>
public sealed record SendPushResultDto(bool Success, int SuccessCount, int FailureCount, string? ErrorMessage = null)
{
    public static SendPushResultDto Succeeded(int successCount) => new(true, successCount, 0);
    public static SendPushResultDto Partial(int successCount, int failureCount) => new(true, successCount, failureCount);
    public static SendPushResultDto Failed(string errorMessage) => new(false, 0, 0, errorMessage);
}
