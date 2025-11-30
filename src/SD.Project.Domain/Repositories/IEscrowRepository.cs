using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for escrow payment persistence operations.
/// </summary>
public interface IEscrowRepository
{
    /// <summary>
    /// Gets an escrow payment by ID.
    /// </summary>
    Task<EscrowPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an escrow payment by order ID.
    /// </summary>
    Task<EscrowPayment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all escrow payments for a buyer.
    /// </summary>
    Task<IReadOnlyList<EscrowPayment>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all escrow allocations for a specific store.
    /// </summary>
    Task<IReadOnlyList<EscrowAllocation>> GetAllocationsByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets escrow allocations by shipment ID.
    /// </summary>
    Task<EscrowAllocation?> GetAllocationByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets escrow allocation by ID.
    /// </summary>
    Task<EscrowAllocation?> GetAllocationByIdAsync(
        Guid allocationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all escrow allocations that are eligible for payout.
    /// </summary>
    Task<IReadOnlyList<EscrowAllocation>> GetEligibleForPayoutAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets escrow allocations for a store that are eligible for payout.
    /// </summary>
    Task<IReadOnlyList<EscrowAllocation>> GetEligibleForPayoutByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all held escrow allocations for a store (pending payout).
    /// </summary>
    Task<IReadOnlyList<EscrowAllocation>> GetHeldAllocationsByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets released escrow allocations for a store with pagination.
    /// </summary>
    Task<(IReadOnlyList<EscrowAllocation> Allocations, int TotalCount)> GetReleasedAllocationsByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new escrow payment.
    /// </summary>
    Task AddAsync(EscrowPayment escrowPayment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing escrow payment.
    /// </summary>
    Task UpdateAsync(EscrowPayment escrowPayment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a ledger entry for audit.
    /// </summary>
    Task AddLedgerEntryAsync(EscrowLedger ledgerEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ledger entries for an escrow payment.
    /// </summary>
    Task<IReadOnlyList<EscrowLedger>> GetLedgerEntriesAsync(
        Guid escrowPaymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ledger entries for a store with pagination.
    /// </summary>
    Task<(IReadOnlyList<EscrowLedger> Entries, int TotalCount)> GetLedgerEntriesByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
