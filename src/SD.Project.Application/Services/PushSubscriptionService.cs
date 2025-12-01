using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating push subscription use cases.
/// </summary>
public sealed class PushSubscriptionService
{
    private readonly IPushSubscriptionRepository _repository;
    private readonly IPushNotificationService _pushService;

    public PushSubscriptionService(
        IPushSubscriptionRepository repository,
        IPushNotificationService pushService)
    {
        _repository = repository;
        _pushService = pushService;
    }

    /// <summary>
    /// Gets the VAPID public key for client-side subscription.
    /// </summary>
    public VapidPublicKeyDto HandleAsync(GetVapidPublicKeyQuery query)
    {
        var publicKey = _pushService.GetVapidPublicKey();
        return new VapidPublicKeyDto(publicKey);
    }

    /// <summary>
    /// Gets all push subscriptions for a user.
    /// </summary>
    public async Task<IReadOnlyList<PushSubscriptionDto>> HandleAsync(
        GetPushSubscriptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var subscriptions = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        return subscriptions.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Checks if a user has any active push subscriptions.
    /// </summary>
    public async Task<bool> HandleAsync(
        HasPushSubscriptionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var subscriptions = await _repository.GetEnabledByUserIdAsync(query.UserId, cancellationToken);
        return subscriptions.Count > 0;
    }

    /// <summary>
    /// Subscribes a user's device to push notifications.
    /// </summary>
    public async Task<PushSubscriptionResultDto> HandleAsync(
        SubscribeToPushCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if subscription already exists for this endpoint
        var existing = await _repository.GetByEndpointAsync(command.Endpoint, cancellationToken);
        
        if (existing is not null)
        {
            // Update keys if the subscription exists but belongs to the same user
            if (existing.UserId == command.UserId)
            {
                existing.UpdateKeys(command.P256dh, command.Auth);
                existing.Enable();
                _repository.Update(existing);
                await _repository.SaveChangesAsync(cancellationToken);
                return PushSubscriptionResultDto.Succeeded(existing.Id);
            }
            
            // Different user - delete old subscription and create new one
            _repository.Delete(existing);
        }

        var subscription = new PushSubscription(
            Guid.NewGuid(),
            command.UserId,
            command.Endpoint,
            command.P256dh,
            command.Auth,
            command.DeviceName);

        await _repository.AddAsync(subscription, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return PushSubscriptionResultDto.Succeeded(subscription.Id);
    }

    /// <summary>
    /// Unsubscribes from push notifications by endpoint.
    /// </summary>
    public async Task<PushUnsubscribeResultDto> HandleAsync(
        UnsubscribeFromPushCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await _repository.GetByEndpointAsync(command.Endpoint, cancellationToken);
        
        if (subscription is null)
        {
            return PushUnsubscribeResultDto.Failed("Subscription not found.");
        }

        if (subscription.UserId != command.UserId)
        {
            return PushUnsubscribeResultDto.Failed("Subscription not found.");
        }

        _repository.Delete(subscription);
        await _repository.SaveChangesAsync(cancellationToken);

        return PushUnsubscribeResultDto.Succeeded();
    }

    /// <summary>
    /// Enables or disables a specific push subscription.
    /// </summary>
    public async Task<PushSubscriptionResultDto> HandleAsync(
        TogglePushSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await _repository.GetByIdAsync(command.SubscriptionId, cancellationToken);
        
        if (subscription is null || subscription.UserId != command.UserId)
        {
            return PushSubscriptionResultDto.Failed("Subscription not found.");
        }

        if (command.Enable)
        {
            subscription.Enable();
        }
        else
        {
            subscription.Disable();
        }

        _repository.Update(subscription);
        await _repository.SaveChangesAsync(cancellationToken);

        return PushSubscriptionResultDto.Succeeded(subscription.Id);
    }

    /// <summary>
    /// Deletes a specific push subscription.
    /// </summary>
    public async Task<PushUnsubscribeResultDto> HandleAsync(
        DeletePushSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await _repository.GetByIdAsync(command.SubscriptionId, cancellationToken);
        
        if (subscription is null || subscription.UserId != command.UserId)
        {
            return PushUnsubscribeResultDto.Failed("Subscription not found.");
        }

        _repository.Delete(subscription);
        await _repository.SaveChangesAsync(cancellationToken);

        return PushUnsubscribeResultDto.Succeeded();
    }

    private static PushSubscriptionDto MapToDto(PushSubscription s) => new(
        s.Id,
        s.UserId,
        s.Endpoint,
        s.DeviceName,
        s.IsEnabled,
        s.CreatedAt,
        s.LastUsedAt);
}
