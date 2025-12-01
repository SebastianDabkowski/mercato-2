using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for delivery address persistence operations.
/// </summary>
public interface IDeliveryAddressRepository
{
    /// <summary>
    /// Gets a delivery address by its ID.
    /// </summary>
    Task<DeliveryAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active addresses for a buyer.
    /// </summary>
    Task<IReadOnlyList<DeliveryAddress>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default address for a buyer.
    /// </summary>
    Task<DeliveryAddress?> GetDefaultByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets addresses by session ID (for guest checkout).
    /// </summary>
    Task<IReadOnlyList<DeliveryAddress>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new delivery address.
    /// </summary>
    Task AddAsync(DeliveryAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing delivery address.
    /// </summary>
    Task UpdateAsync(DeliveryAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a delivery address.
    /// </summary>
    void Remove(DeliveryAddress address);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
