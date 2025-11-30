using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for refund persistence operations.
/// </summary>
public interface IRefundRepository
{
    /// <summary>
    /// Gets a refund by ID.
    /// </summary>
    Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refunds for an order.
    /// </summary>
    Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refunds for a shipment.
    /// </summary>
    Task<IReadOnlyList<Refund>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refunds by status with pagination.
    /// </summary>
    Task<(IReadOnlyList<Refund> Refunds, int TotalCount)> GetByStatusAsync(
        RefundStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refunds for a store with optional status filter and pagination.
    /// </summary>
    Task<(IReadOnlyList<Refund> Refunds, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        RefundStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total refunded amount for an order.
    /// </summary>
    Task<decimal> GetTotalRefundedAmountAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a refund with the given idempotency key already exists.
    /// </summary>
    Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refund.
    /// </summary>
    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing refund.
    /// </summary>
    Task UpdateAsync(Refund refund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
