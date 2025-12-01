using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for shipping label operations.
/// </summary>
public interface IShippingLabelRepository
{
    /// <summary>
    /// Gets a shipping label by its ID.
    /// </summary>
    Task<ShippingLabel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active (non-voided) shipping label for a shipment.
    /// </summary>
    Task<ShippingLabel?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all labels for a shipment (including voided ones).
    /// </summary>
    Task<IReadOnlyList<ShippingLabel>> GetAllByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all labels for an order.
    /// </summary>
    Task<IReadOnlyList<ShippingLabel>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired labels that should be cleaned up.
    /// </summary>
    Task<IReadOnlyList<ShippingLabel>> GetExpiredLabelsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new shipping label.
    /// </summary>
    Task AddAsync(ShippingLabel label, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing shipping label.
    /// </summary>
    Task UpdateAsync(ShippingLabel label, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a shipping label.
    /// </summary>
    Task DeleteAsync(ShippingLabel label, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
