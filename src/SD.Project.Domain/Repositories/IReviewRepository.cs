using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Statistics for review moderation.
/// </summary>
public record ReviewModerationStats(
    int PendingCount,
    int FlaggedCount,
    int ReportedCount,
    int ApprovedTodayCount,
    int RejectedTodayCount);

/// <summary>
/// Contract for review persistence operations.
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Gets a review by ID.
    /// </summary>
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a review by order ID, shipment ID, and product ID.
    /// Used to check if a review already exists.
    /// </summary>
    Task<Review?> GetByOrderShipmentProductAsync(
        Guid orderId,
        Guid shipmentId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews for a specific product (only approved reviews).
    /// </summary>
    Task<IReadOnlyList<Review>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated and sorted reviews for a specific product (only approved reviews).
    /// </summary>
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductIdPagedAsync(
        Guid productId,
        ReviewSortOrder sortOrder,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews for a specific store (only approved reviews).
    /// </summary>
    Task<IReadOnlyList<Review>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews submitted by a buyer.
    /// </summary>
    Task<IReadOnlyList<Review>> GetByBuyerIdAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of reviews submitted by a buyer in the specified time window.
    /// Used for rate limiting.
    /// </summary>
    Task<int> GetReviewCountByBuyerInWindowAsync(
        Guid buyerId,
        DateTime windowStart,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending reviews for moderation.
    /// </summary>
    Task<IReadOnlyList<Review>> GetPendingReviewsAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated reviews for moderation with filtering options.
    /// </summary>
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetForModerationPagedAsync(
        ReviewModerationStatus? status,
        bool? isFlagged,
        string? searchTerm,
        Guid? storeId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flagged reviews for moderation.
    /// </summary>
    Task<IReadOnlyList<Review>> GetFlaggedReviewsAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reviews with high report count for moderation.
    /// </summary>
    Task<IReadOnlyList<Review>> GetReportedReviewsAsync(
        int minReportCount,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets moderation statistics.
    /// </summary>
    Task<ReviewModerationStats> GetModerationStatsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a product.
    /// </summary>
    Task<(double AverageRating, int ReviewCount)> GetProductRatingAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a store.
    /// </summary>
    Task<(double AverageRating, int ReviewCount)> GetStoreRatingAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new review.
    /// </summary>
    Task AddAsync(Review review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing review.
    /// </summary>
    void Update(Review review);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
