namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the moderation status of a seller rating.
/// </summary>
public enum SellerRatingModerationStatus
{
    /// <summary>Rating is awaiting moderation.</summary>
    Pending,
    /// <summary>Rating has been approved and is publicly visible.</summary>
    Approved,
    /// <summary>Rating has been rejected by moderators.</summary>
    Rejected
}

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
    /// Moderation status of the rating.
    /// </summary>
    public SellerRatingModerationStatus ModerationStatus { get; private set; }

    /// <summary>
    /// Reason for rejection if the rating was rejected.
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Timestamp when the rating was submitted.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the rating was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the rating was moderated (approved or rejected).
    /// </summary>
    public DateTime? ModeratedAt { get; private set; }

    /// <summary>
    /// Indicates whether the rating has been flagged for moderation.
    /// </summary>
    public bool IsFlagged { get; private set; }

    /// <summary>
    /// Reason why the rating was flagged.
    /// </summary>
    public string? FlagReason { get; private set; }

    /// <summary>
    /// Timestamp when the rating was flagged.
    /// </summary>
    public DateTime? FlaggedAt { get; private set; }

    /// <summary>
    /// Number of times this rating has been reported by users.
    /// </summary>
    public int ReportCount { get; private set; }

    /// <summary>
    /// ID of the moderator who last moderated this rating.
    /// </summary>
    public Guid? ModeratedByUserId { get; private set; }

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
        ModerationStatus = SellerRatingModerationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rating.
    /// Only allowed while the rating is pending moderation.
    /// </summary>
    public void Update(int rating, string? comment)
    {
        if (ModerationStatus != SellerRatingModerationStatus.Pending)
        {
            throw new InvalidOperationException("Cannot update a rating that has already been moderated.");
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
    /// Checks if the rating is publicly visible.
    /// </summary>
    public bool IsVisible => ModerationStatus == SellerRatingModerationStatus.Approved;

    /// <summary>
    /// Approves the rating with moderator tracking.
    /// </summary>
    public void ApproveByModerator(Guid moderatorId)
    {
        if (moderatorId == Guid.Empty)
        {
            throw new ArgumentException("Moderator ID is required.", nameof(moderatorId));
        }

        ModerationStatus = SellerRatingModerationStatus.Approved;
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
    /// Rejects the rating with moderator tracking.
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

        ModerationStatus = SellerRatingModerationStatus.Rejected;
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
    /// Flags the rating for moderation.
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
    /// Clears the flag on the rating.
    /// </summary>
    public void ClearFlag()
    {
        IsFlagged = false;
        FlagReason = null;
        FlaggedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the report count when a user reports this rating.
    /// </summary>
    public void Report()
    {
        ReportCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the moderation status to pending (for re-review).
    /// </summary>
    public void ResetToPending()
    {
        ModerationStatus = SellerRatingModerationStatus.Pending;
        ModeratedAt = null;
        ModeratedByUserId = null;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
