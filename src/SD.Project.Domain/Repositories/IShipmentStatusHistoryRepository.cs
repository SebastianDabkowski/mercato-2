using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for shipment status history persistence operations.
/// </summary>
public interface IShipmentStatusHistoryRepository
{
    /// <summary>
    /// Adds a new status history record.
    /// </summary>
    Task AddAsync(ShipmentStatusHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all status history records for a specific shipment.
    /// </summary>
    Task<IReadOnlyList<ShipmentStatusHistory>> GetByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all status history records for a specific order (all shipments).
    /// </summary>
    Task<IReadOnlyList<ShipmentStatusHistory>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
