using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for seller reputation persistence operations.
/// </summary>
public interface ISellerReputationRepository
{
    /// <summary>
    /// Gets a seller reputation by ID.
    /// </summary>
    Task<SellerReputation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a seller reputation by store ID.
    /// </summary>
    Task<SellerReputation?> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets seller reputations for multiple stores.
    /// </summary>
    Task<IReadOnlyList<SellerReputation>> GetByStoreIdsAsync(
        IEnumerable<Guid> storeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all seller reputations ordered by score descending.
    /// </summary>
    Task<IReadOnlyList<SellerReputation>> GetTopSellersAsync(
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets seller reputations that need recalculation.
    /// Returns reputations not updated since the specified time.
    /// </summary>
    Task<IReadOnlyList<SellerReputation>> GetStaleReputationsAsync(
        DateTime olderThan,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new seller reputation.
    /// </summary>
    Task AddAsync(SellerReputation reputation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing seller reputation.
    /// </summary>
    void Update(SellerReputation reputation);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
