using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for seller rating persistence operations.
/// </summary>
public interface ISellerRatingRepository
{
    /// <summary>
    /// Gets a seller rating by ID.
    /// </summary>
    Task<SellerRating?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a seller rating by order ID.
    /// Used to check if a rating already exists for an order.
    /// </summary>
    Task<SellerRating?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ratings for a specific store.
    /// </summary>
    Task<IReadOnlyList<SellerRating>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating and count for a store.
    /// This is used to calculate the seller's reputation score.
    /// </summary>
    Task<(double AverageRating, int RatingCount)> GetStoreRatingStatsAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new seller rating.
    /// </summary>
    Task AddAsync(SellerRating rating, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing seller rating.
    /// </summary>
    void Update(SellerRating rating);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
