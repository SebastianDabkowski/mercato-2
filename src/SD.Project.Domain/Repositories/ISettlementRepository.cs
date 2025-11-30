using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for settlement persistence operations.
/// </summary>
public interface ISettlementRepository
{
    /// <summary>
    /// Gets a settlement by ID.
    /// </summary>
    Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement for a specific store and period.
    /// </summary>
    Task<Settlement?> GetByStoreAndPeriodAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest settlement for a store and period (highest version).
    /// </summary>
    Task<Settlement?> GetLatestByStoreAndPeriodAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements for a store.
    /// </summary>
    Task<IReadOnlyList<Settlement>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements for a store with pagination.
    /// </summary>
    Task<(IReadOnlyList<Settlement> Settlements, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements for a specific period across all stores.
    /// </summary>
    Task<IReadOnlyList<Settlement>> GetByPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements with filtering and pagination for admin view.
    /// </summary>
    Task<(IReadOnlyList<Settlement> Settlements, int TotalCount)> GetFilteredAsync(
        Guid? storeId,
        int? year,
        int? month,
        SettlementStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next version number for a store and period.
    /// </summary>
    Task<int> GetNextVersionAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a settlement exists for a store and period (any version).
    /// </summary>
    Task<bool> ExistsForPeriodAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stores that have escrow activity for a period but no settlement.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetStoresWithoutSettlementAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new settlement.
    /// </summary>
    Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing settlement.
    /// </summary>
    Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
