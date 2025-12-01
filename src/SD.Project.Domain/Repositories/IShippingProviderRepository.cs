using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for shipping provider persistence operations.
/// </summary>
public interface IShippingProviderRepository
{
    /// <summary>
    /// Gets a shipping provider by ID.
    /// </summary>
    Task<ShippingProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shipping providers for a store.
    /// </summary>
    Task<IReadOnlyList<ShippingProvider>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled shipping providers for a store.
    /// </summary>
    Task<IReadOnlyList<ShippingProvider>> GetEnabledByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all platform-wide shipping providers.
    /// </summary>
    Task<IReadOnlyList<ShippingProvider>> GetPlatformProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shipping provider by store and provider type.
    /// </summary>
    Task<ShippingProvider?> GetByStoreAndTypeAsync(
        Guid storeId,
        ShippingProviderType providerType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new shipping provider.
    /// </summary>
    Task AddAsync(ShippingProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing shipping provider.
    /// </summary>
    Task UpdateAsync(ShippingProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a shipping provider.
    /// </summary>
    Task DeleteAsync(ShippingProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
