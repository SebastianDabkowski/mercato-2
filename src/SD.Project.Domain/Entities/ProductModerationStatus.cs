namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the moderation state of a product for admin review.
/// </summary>
public enum ProductModerationStatus
{
    /// <summary>
    /// Product is awaiting moderation review.
    /// </summary>
    PendingReview = 0,

    /// <summary>
    /// Product has been approved by a moderator and can be made active.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Product has been rejected by a moderator.
    /// </summary>
    Rejected = 2
}
