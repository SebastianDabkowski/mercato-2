namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the moderation state of a product photo.
/// </summary>
public enum PhotoModerationStatus
{
    /// <summary>
    /// Photo is awaiting moderation review.
    /// </summary>
    PendingReview = 0,

    /// <summary>
    /// Photo has been approved by a moderator and is visible on the product page.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Photo has been removed by a moderator (not visible on the product page).
    /// Removed photos are archived rather than hard-deleted for legal retention.
    /// </summary>
    Removed = 2
}
