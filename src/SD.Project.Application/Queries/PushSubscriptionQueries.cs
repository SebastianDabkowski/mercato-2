namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get the VAPID public key for push subscription.
/// </summary>
public sealed record GetVapidPublicKeyQuery;

/// <summary>
/// Query to get all push subscriptions for a user.
/// </summary>
public sealed record GetPushSubscriptionsQuery(Guid UserId);

/// <summary>
/// Query to check if a user has any active push subscriptions.
/// </summary>
public sealed record HasPushSubscriptionQuery(Guid UserId);
