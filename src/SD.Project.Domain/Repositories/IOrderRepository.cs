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
}
