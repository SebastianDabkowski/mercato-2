namespace SD.Project.Application.Commands;

/// <summary>
/// Command to subscribe a user's device to push notifications.
/// </summary>
public sealed record SubscribeToPushCommand(
    Guid UserId,
    string Endpoint,
    string P256dh,
    string Auth,
    string? DeviceName = null);

/// <summary>
/// Command to unsubscribe from push notifications by endpoint.
/// </summary>
public sealed record UnsubscribeFromPushCommand(
    Guid UserId,
    string Endpoint);

/// <summary>
/// Command to enable or disable push notifications for a specific subscription.
/// </summary>
public sealed record TogglePushSubscriptionCommand(
    Guid UserId,
    Guid SubscriptionId,
    bool Enable);

/// <summary>
/// Command to delete a specific push subscription.
/// </summary>
public sealed record DeletePushSubscriptionCommand(
    Guid UserId,
    Guid SubscriptionId);
