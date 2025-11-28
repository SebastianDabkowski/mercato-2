using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for shipping method persistence operations.
/// </summary>
public interface IShippingMethodRepository
{
    /// <summary>
    /// Gets a shipping method by ID.
    /// </summary>
    Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active shipping methods for a store.
    /// </summary>
    Task<IReadOnlyList<ShippingMethod>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active shipping methods for multiple stores.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ShippingMethod>>> GetByStoreIdsAsync(
        IEnumerable<Guid> storeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default shipping method for a store.
    /// </summary>
    Task<ShippingMethod?> GetDefaultByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets platform-wide shipping methods (not store-specific).
    /// </summary>
    Task<IReadOnlyList<ShippingMethod>> GetPlatformMethodsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new shipping method.
    /// </summary>
    Task AddAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing shipping method.
    /// </summary>
    Task UpdateAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
