using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for managing push subscriptions.
/// </summary>
public interface IPushSubscriptionRepository
{
    /// <summary>
    /// Gets a push subscription by its ID.
    /// </summary>
    Task<PushSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a push subscription by endpoint.
    /// </summary>
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all push subscriptions for a user.
    /// </summary>
    Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled push subscriptions for a user.
    /// </summary>
    Task<IReadOnlyList<PushSubscription>> GetEnabledByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new push subscription.
    /// </summary>
    Task AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing push subscription.
    /// </summary>
    void Update(PushSubscription subscription);

    /// <summary>
    /// Deletes a push subscription.
    /// </summary>
    void Delete(PushSubscription subscription);

    /// <summary>
    /// Saves changes to the repository.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
