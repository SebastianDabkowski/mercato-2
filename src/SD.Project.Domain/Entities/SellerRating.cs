namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a seller rating submitted by a buyer after an order is delivered.
/// Each order can have only one seller rating.
/// </summary>
public class SellerRating
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The order ID that this rating is associated with.
    /// Ratings can only be submitted for delivered orders.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The store/seller being rated.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The buyer who submitted the rating.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// Rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; private set; }

    /// <summary>
    /// Optional feedback from the buyer.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Timestamp when the rating was submitted.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the rating was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private SellerRating()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new seller rating.
    /// </summary>
    public SellerRating(
        Guid orderId,
        Guid storeId,
        Guid buyerId,
        int rating,
        string? comment)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        StoreId = storeId;
        BuyerId = buyerId;
        Rating = rating;
        Comment = comment?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rating.
    /// </summary>
    public void Update(int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));
        }

        Rating = rating;
        Comment = comment?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
