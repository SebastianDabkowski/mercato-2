namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a review.
/// </summary>
public sealed record ReviewViewModel(
    Guid ReviewId,
    Guid ProductId,
    string? BuyerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

/// <summary>
/// View model for displaying product rating summary.
/// </summary>
public sealed record ProductRatingViewModel(
    double AverageRating,
    int ReviewCount)
{
    /// <summary>
    /// Gets the rating display as a string with one decimal place.
    /// </summary>
    public string RatingDisplay => AverageRating.ToString("0.0");
}

/// <summary>
/// View model for submitting a review.
/// </summary>
public sealed record SubmitReviewViewModel
{
    public Guid OrderId { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? StoreName { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
}

/// <summary>
/// View model for review eligibility info displayed on order details.
/// </summary>
public sealed record ReviewEligibilityViewModel(
    bool IsEligible,
    string? IneligibilityReason,
    bool HasExistingReview);

/// <summary>
/// View model for seller rating eligibility info displayed on order details.
/// </summary>
public sealed record SellerRatingEligibilityViewModel(
    bool IsEligible,
    string? IneligibilityReason,
    bool HasExistingRating);
