using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for order persistence operations.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for a buyer.
    /// </summary>
    Task<IReadOnlyList<Order>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent orders for a buyer with pagination.
    /// </summary>
    Task<IReadOnlyList<Order>> GetRecentByBuyerIdAsync(
        Guid buyerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets filtered and paginated orders for a specific buyer.
    /// </summary>
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetFilteredByBuyerIdAsync(
        Guid buyerId,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? sellerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new order.
    /// </summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique order number.
    /// </summary>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shipments (sub-orders) for a specific store.
    /// </summary>
    Task<IReadOnlyList<OrderShipment>> GetShipmentsByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets filtered and paginated shipments (sub-orders) for a specific store.
    /// </summary>
    Task<(IReadOnlyList<OrderShipment> Shipments, int TotalCount)> GetFilteredShipmentsByStoreIdAsync(
        Guid storeId,
        ShipmentStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? buyerSearch,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shipments for a store with their associated orders for export.
    /// </summary>
    Task<IReadOnlyList<(OrderShipment Shipment, Order Order, IReadOnlyList<OrderItem> Items)>> GetAllShipmentsForExportAsync(
        Guid storeId,
        ShipmentStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? buyerSearch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific shipment by ID with its associated order and items.
    /// </summary>
    Task<(OrderShipment? Shipment, Order? Order, IReadOnlyList<OrderItem> Items)> GetShipmentWithOrderAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an order with the given payment transaction ID already exists.
    /// Used for idempotency to prevent duplicate orders from payment callbacks.
    /// </summary>
    Task<bool> ExistsByPaymentTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
}
