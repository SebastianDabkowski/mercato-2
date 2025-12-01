namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a seller rating.
/// </summary>
public record SellerRatingDto(
    Guid RatingId,
    Guid OrderId,
    Guid StoreId,
    Guid BuyerId,
    string? BuyerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

/// <summary>
/// DTO for seller rating statistics (contributes to reputation score).
/// </summary>
public record SellerRatingStatsDto(
    Guid StoreId,
    double AverageRating,
    int RatingCount);

/// <summary>
/// Result of submitting a seller rating.
/// </summary>
public record SubmitSellerRatingResultDto(
    bool Success,
    string? ErrorMessage = null,
    Guid? RatingId = null);

/// <summary>
/// DTO for checking seller rating eligibility.
/// </summary>
public record SellerRatingEligibilityDto(
    bool IsEligible,
    string? IneligibilityReason = null,
    bool HasExistingRating = false);
