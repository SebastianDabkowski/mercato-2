namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the moderation status of a review.
/// </summary>
public enum ReviewModerationStatus
{
    /// <summary>Review is awaiting moderation.</summary>
    Pending,
    /// <summary>Review has been approved and is publicly visible.</summary>
    Approved,
    /// <summary>Review has been rejected by moderators.</summary>
    Rejected
}

/// <summary>
/// Represents a product review submitted by a buyer after order delivery.
/// </summary>
public class Review
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The order ID that this review is associated with.
    /// Reviews can only be submitted for delivered orders.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The shipment ID (sub-order) within the order that this review is for.
    /// </summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>
    /// The product being reviewed.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// The store/seller being reviewed.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The buyer who submitted the review.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// Rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; private set; }

    /// <summary>
    /// Optional text feedback from the buyer.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Moderation status of the review.
    /// </summary>
    public ReviewModerationStatus ModerationStatus { get; private set; }

    /// <summary>
    /// Reason for rejection if the review was rejected.
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Timestamp when the review was submitted.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the review was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the review was moderated (approved or rejected).
    /// </summary>
    public DateTime? ModeratedAt { get; private set; }

    private Review()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new review.
    /// </summary>
    public Review(
        Guid orderId,
        Guid shipmentId,
        Guid productId,
        Guid storeId,
        Guid buyerId,
        int rating,
        string? comment)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
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
        ShipmentId = shipmentId;
        ProductId = productId;
        StoreId = storeId;
        BuyerId = buyerId;
        Rating = rating;
        Comment = comment?.Trim();
        ModerationStatus = ReviewModerationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the review content.
    /// Only allowed while the review is pending moderation.
    /// </summary>
    public void Update(int rating, string? comment)
    {
        if (ModerationStatus != ReviewModerationStatus.Pending)
        {
            throw new InvalidOperationException("Cannot update a review that has already been moderated.");
        }

        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));
        }

        Rating = rating;
        Comment = comment?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the review, making it publicly visible.
    /// </summary>
    public void Approve()
    {
        if (ModerationStatus != ReviewModerationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reviews can be approved.");
        }

        ModerationStatus = ReviewModerationStatus.Approved;
        ModeratedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the review with a reason.
    /// </summary>
    public void Reject(string reason)
    {
        if (ModerationStatus != ReviewModerationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reviews can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        ModerationStatus = ReviewModerationStatus.Rejected;
        RejectionReason = reason.Trim();
        ModeratedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the review is publicly visible.
    /// </summary>
    public bool IsVisible => ModerationStatus == ReviewModerationStatus.Approved;
}
