using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for seller payout persistence operations.
/// </summary>
public interface ISellerPayoutRepository
{
    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    Task<SellerPayout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payouts for a store.
    /// </summary>
    Task<IReadOnlyList<SellerPayout>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payouts for a store with pagination.
    /// </summary>
    Task<(IReadOnlyList<SellerPayout> Payouts, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payouts by status.
    /// </summary>
    Task<IReadOnlyList<SellerPayout>> GetByStatusAsync(
        SellerPayoutStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payouts that are scheduled for processing on or before the specified date.
    /// </summary>
    Task<IReadOnlyList<SellerPayout>> GetScheduledForProcessingAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed payouts that are due for retry.
    /// </summary>
    Task<IReadOnlyList<SellerPayout>> GetDueForRetryAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current scheduled payout for a store (if any).
    /// Returns the most recent scheduled payout that hasn't been processed yet.
    /// </summary>
    Task<SellerPayout?> GetCurrentScheduledPayoutAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an escrow allocation is already included in a payout.
    /// </summary>
    Task<bool> IsAllocationInPayoutAsync(
        Guid escrowAllocationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payout.
    /// </summary>
    Task AddAsync(SellerPayout payout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payout.
    /// </summary>
    Task UpdateAsync(SellerPayout payout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
