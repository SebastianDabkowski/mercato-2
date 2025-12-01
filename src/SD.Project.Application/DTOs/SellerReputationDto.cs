using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying a seller's reputation score and metrics.
/// </summary>
public record SellerReputationDto(
    Guid StoreId,
    decimal ReputationScore,
    ReputationTier ReputationTier,
    decimal AverageRating,
    int TotalRatings,
    decimal OnTimeShippingRate,
    decimal CancellationRate,
    decimal DisputeRate,
    DateTime LastCalculatedAt);

/// <summary>
/// Simplified DTO for buyer-facing reputation display.
/// Shows only the metrics buyers care about.
/// </summary>
public record SellerReputationSummaryDto(
    Guid StoreId,
    decimal ReputationScore,
    ReputationTier ReputationTier,
    decimal AverageRating,
    int TotalRatings,
    decimal OnTimeShippingRate);

/// <summary>
/// Result of recalculating a seller's reputation score.
/// </summary>
public record RecalculateReputationResultDto(
    bool Success,
    string? ErrorMessage = null,
    SellerReputationDto? Reputation = null);

/// <summary>
/// Result of batch reputation recalculation.
/// </summary>
public record BatchRecalculateReputationResultDto(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> Errors);
