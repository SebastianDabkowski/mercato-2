namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a review.
/// </summary>
public record ReviewDto(
    Guid ReviewId,
    Guid ProductId,
    Guid StoreId,
    Guid BuyerId,
    string? BuyerName,
    int Rating,
    string? Comment,
    string ModerationStatus,
    DateTime CreatedAt);

/// <summary>
/// DTO for product rating summary.
/// </summary>
public record ProductRatingDto(
    Guid ProductId,
    double AverageRating,
    int ReviewCount);

/// <summary>
/// DTO for store rating summary.
/// </summary>
public record StoreRatingDto(
    Guid StoreId,
    double AverageRating,
    int ReviewCount);

/// <summary>
/// Result of submitting a review.
/// </summary>
public record SubmitReviewResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? ReviewId = null);

/// <summary>
/// DTO for checking review eligibility.
/// </summary>
public record ReviewEligibilityDto(
    bool IsEligible,
    string? IneligibilityReason = null,
    bool HasExistingReview = false);

/// <summary>
/// Result of reporting a review.
/// </summary>
public record ReportReviewResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? ReportId = null);
