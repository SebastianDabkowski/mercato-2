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

    /// <summary>
    /// Indicates whether the review has been flagged for moderation.
    /// </summary>
    public bool IsFlagged { get; private set; }

    /// <summary>
    /// Reason why the review was flagged.
    /// </summary>
    public string? FlagReason { get; private set; }

    /// <summary>
    /// Timestamp when the review was flagged.
    /// </summary>
    public DateTime? FlaggedAt { get; private set; }

    /// <summary>
    /// Number of times this review has been reported by users.
    /// </summary>
    public int ReportCount { get; private set; }

    /// <summary>
    /// ID of the moderator who last moderated this review.
    /// </summary>
    public Guid? ModeratedByUserId { get; private set; }

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

    /// <summary>
    /// Flags the review for moderation.
    /// </summary>
    public void Flag(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Flag reason is required.", nameof(reason));
        }

        IsFlagged = true;
        FlagReason = reason.Trim();
        FlaggedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears the flag on the review.
    /// </summary>
    public void ClearFlag()
    {
        IsFlagged = false;
        FlagReason = null;
        FlaggedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the report count when a user reports this review.
    /// </summary>
    public void Report()
    {
        ReportCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the review with moderator tracking.
    /// </summary>
    public void ApproveByModerator(Guid moderatorId)
    {
        if (moderatorId == Guid.Empty)
        {
            throw new ArgumentException("Moderator ID is required.", nameof(moderatorId));
        }

        ModerationStatus = ReviewModerationStatus.Approved;
        ModeratedAt = DateTime.UtcNow;
        ModeratedByUserId = moderatorId;
        UpdatedAt = DateTime.UtcNow;
        
        // Clear flag if it was set
        if (IsFlagged)
        {
            ClearFlag();
        }
    }

    /// <summary>
    /// Rejects the review with moderator tracking.
    /// </summary>
    public void RejectByModerator(Guid moderatorId, string reason)
    {
        if (moderatorId == Guid.Empty)
        {
            throw new ArgumentException("Moderator ID is required.", nameof(moderatorId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        ModerationStatus = ReviewModerationStatus.Rejected;
        RejectionReason = reason.Trim();
        ModeratedAt = DateTime.UtcNow;
        ModeratedByUserId = moderatorId;
        UpdatedAt = DateTime.UtcNow;
        
        // Clear flag if it was set
        if (IsFlagged)
        {
            ClearFlag();
        }
    }

    /// <summary>
    /// Resets the moderation status to pending (for re-review).
    /// </summary>
    public void ResetToPending()
    {
        ModerationStatus = ReviewModerationStatus.Pending;
        ModeratedAt = null;
        ModeratedByUserId = null;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the review as anonymized for GDPR compliance.
    /// Note: The actual anonymization happens at the User entity level - the User's 
    /// FirstName and LastName are changed to "Deleted" and "User" respectively.
    /// The BuyerId reference is preserved for data integrity and referential consistency.
    /// When displaying reviews, the UI should look up the buyer's name from the User entity,
    /// which will show "Deleted User" for anonymized accounts.
    /// </summary>
    public void AnonymizeAuthor()
    {
        // The BuyerId reference is kept for data integrity.
        // The anonymized user entity will have FirstName="Deleted" and LastName="User"
        // which the UI will use to display the author name.
        // Update timestamp to indicate the record was modified during anonymization.
        UpdatedAt = DateTime.UtcNow;
    }
}
