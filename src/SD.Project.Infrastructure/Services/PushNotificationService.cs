using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Repositories;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Implementation of push notification service using Web Push protocol.
/// This is a stub implementation that logs notifications for development.
/// In production, integrate with WebPush library (e.g., Lib.Net.Http.WebPush).
/// </summary>
public sealed class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IPushSubscriptionRepository _subscriptionRepository;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;

    public PushNotificationService(
        ILogger<PushNotificationService> logger,
        IPushSubscriptionRepository subscriptionRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _subscriptionRepository = subscriptionRepository;

        // Load VAPID keys from configuration
        // These are required for Web Push. Generate using: https://vapidkeys.com/
        _vapidPublicKey = configuration["PushNotifications:VapidPublicKey"] ?? "";
        _vapidPrivateKey = configuration["PushNotifications:VapidPrivateKey"] ?? "";
        _vapidSubject = configuration["PushNotifications:VapidSubject"] ?? "mailto:support@mercato.com";

        if (string.IsNullOrEmpty(_vapidPublicKey))
        {
            _logger.LogWarning("VAPID public key not configured. Push notifications will not work.");
        }
    }

    public string GetVapidPublicKey()
    {
        return _vapidPublicKey;
    }

    public async Task<int> SendToUserAsync(
        Guid userId,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetEnabledByUserIdAsync(userId, cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active push subscriptions for user {UserId}", userId);
            return 0;
        }

        var successCount = 0;
        var failedEndpoints = new List<string>();

        foreach (var subscription in subscriptions)
        {
            var success = await SendToEndpointAsync(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth,
                payload,
                cancellationToken);

            if (success)
            {
                successCount++;
                subscription.MarkAsUsed();
                _subscriptionRepository.Update(subscription);
            }
            else
            {
                failedEndpoints.Add(subscription.Endpoint);
            }
        }

        if (successCount > 0)
        {
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);
        }

        if (failedEndpoints.Count > 0)
        {
            _logger.LogWarning(
                "Failed to send push notifications to {FailureCount} endpoints for user {UserId}",
                failedEndpoints.Count, userId);
        }

        return successCount;
    }

    public Task<bool> SendToEndpointAsync(
        string endpoint,
        string p256dh,
        string auth,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        // In production, use a library like Lib.Net.Http.WebPush to send actual push notifications
        // Example with Lib.Net.Http.WebPush:
        // var subscription = new PushSubscription { Endpoint = endpoint, Keys = { P256DH = p256dh, Auth = auth } };
        // var vapidDetails = new VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);
        // await webPushClient.SendNotificationAsync(subscription, JsonSerializer.Serialize(payload), vapidDetails);

        if (string.IsNullOrEmpty(_vapidPublicKey) || string.IsNullOrEmpty(_vapidPrivateKey))
        {
            _logger.LogDebug(
                "Push notification simulated (VAPID not configured): {Title} - {Body} to {Endpoint}",
                payload.Title, payload.Body, endpoint);
            return Task.FromResult(true);
        }

        // Serialize payload for logging
        var payloadJson = JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Body,
            icon = payload.Icon,
            badge = payload.Badge,
            data = new
            {
                url = payload.Url,
                tag = payload.Tag,
                customData = payload.Data
            }
        });

        _logger.LogInformation(
            "Sending push notification to endpoint: {Endpoint}. Payload: {Payload}",
            endpoint, payloadJson);

        // For now, return true to simulate success
        // In production, implement actual WebPush sending and return false on 410 (Gone) or network errors
        return Task.FromResult(true);
    }
}
