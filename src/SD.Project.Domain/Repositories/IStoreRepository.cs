using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for Store aggregate.
/// </summary>
public interface IStoreRepository
{
    /// <summary>
    /// Gets a store by its ID.
    /// </summary>
    Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store by its seller ID.
    /// </summary>
    Task<Store?> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store by its name (case-insensitive).
    /// </summary>
    Task<Store?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store by its slug (case-insensitive).
    /// </summary>
    Task<Store?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a store name already exists.
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeStoreId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a store slug already exists.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeStoreId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new store.
    /// </summary>
    Task AddAsync(Store store, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all publicly visible stores (Active and LimitedActive status).
    /// </summary>
    Task<IReadOnlyCollection<Store>> GetPubliclyVisibleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stores by their IDs.
    /// </summary>
    Task<IReadOnlyCollection<Store>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
