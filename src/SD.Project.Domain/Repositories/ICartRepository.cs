using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for cart persistence operations.
/// </summary>
public interface ICartRepository
{
    /// <summary>
    /// Gets a cart by ID including its items.
    /// </summary>
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart by buyer ID including its items.
    /// </summary>
    Task<Cart?> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart by session ID including its items (for anonymous users).
    /// </summary>
    Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new cart.
    /// </summary>
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing cart.
    /// </summary>
    void Update(Cart cart);

    /// <summary>
    /// Deletes a cart.
    /// </summary>
    void Delete(Cart cart);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
