using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for shipping rule persistence operations.
/// </summary>
public interface IShippingRuleRepository
{
    /// <summary>
    /// Gets a shipping rule by ID.
    /// </summary>
    Task<ShippingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shipping rules for a store.
    /// </summary>
    Task<IReadOnlyCollection<ShippingRule>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default shipping rule for a store. Returns null if no default is set.
    /// </summary>
    Task<ShippingRule?> GetDefaultByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default shipping rules for multiple stores in a single query.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ShippingRule>> GetDefaultsByStoreIdsAsync(
        IEnumerable<Guid> storeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new shipping rule.
    /// </summary>
    Task AddAsync(ShippingRule shippingRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing shipping rule.
    /// </summary>
    Task UpdateAsync(ShippingRule shippingRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a shipping rule.
    /// </summary>
    Task DeleteAsync(ShippingRule shippingRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
