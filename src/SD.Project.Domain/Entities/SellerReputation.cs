namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an aggregated reputation score for a seller/store.
/// The score is calculated based on seller ratings, dispute rate, on-time shipping rate, and cancelled orders.
/// This entity is updated periodically (batch or event-driven) based on the seller's activity history.
/// </summary>
public class SellerReputation
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The store ID this reputation is associated with.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The overall reputation score (0-100).
    /// Higher scores indicate more trusted sellers.
    /// </summary>
    public decimal ReputationScore { get; private set; }

    /// <summary>
    /// Average rating from buyer reviews (1-5).
    /// </summary>
    public decimal AverageRating { get; private set; }

    /// <summary>
    /// Total number of ratings received.
    /// </summary>
    public int TotalRatings { get; private set; }

    /// <summary>
    /// Total number of completed orders.
    /// </summary>
    public int TotalCompletedOrders { get; private set; }

    /// <summary>
    /// Total number of cancelled orders.
    /// </summary>
    public int TotalCancelledOrders { get; private set; }

    /// <summary>
    /// Cancellation rate as a percentage (0-100).
    /// </summary>
    public decimal CancellationRate { get; private set; }

    /// <summary>
    /// Total number of disputes/return requests.
    /// </summary>
    public int TotalDisputes { get; private set; }

    /// <summary>
    /// Dispute rate as a percentage (0-100).
    /// </summary>
    public decimal DisputeRate { get; private set; }

    /// <summary>
    /// Number of orders shipped on time.
    /// </summary>
    public int OnTimeShipments { get; private set; }

    /// <summary>
    /// Total shipments for on-time calculation.
    /// </summary>
    public int TotalShipments { get; private set; }

    /// <summary>
    /// On-time shipping rate as a percentage (0-100).
    /// </summary>
    public decimal OnTimeShippingRate { get; private set; }

    /// <summary>
    /// When the reputation was first created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the reputation was last recalculated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// When the reputation was last fully recalculated from historical data.
    /// </summary>
    public DateTime LastCalculatedAt { get; private set; }

    private SellerReputation()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new seller reputation record for a store.
    /// </summary>
    public SellerReputation(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        ReputationScore = 0m;
        AverageRating = 0m;
        TotalRatings = 0;
        TotalCompletedOrders = 0;
        TotalCancelledOrders = 0;
        CancellationRate = 0m;
        TotalDisputes = 0;
        DisputeRate = 0m;
        OnTimeShipments = 0;
        TotalShipments = 0;
        OnTimeShippingRate = 0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        LastCalculatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the reputation metrics and recalculates the overall score.
    /// </summary>
    /// <param name="averageRating">Average rating (1-5).</param>
    /// <param name="totalRatings">Total number of ratings.</param>
    /// <param name="totalCompletedOrders">Total completed orders.</param>
    /// <param name="totalCancelledOrders">Total cancelled orders.</param>
    /// <param name="totalDisputes">Total disputes/return requests.</param>
    /// <param name="onTimeShipments">Number of on-time shipments.</param>
    /// <param name="totalShipments">Total shipments.</param>
    public void UpdateMetrics(
        decimal averageRating,
        int totalRatings,
        int totalCompletedOrders,
        int totalCancelledOrders,
        int totalDisputes,
        int onTimeShipments,
        int totalShipments)
    {
        if (averageRating < 0m || averageRating > 5m)
        {
            throw new ArgumentException("Average rating must be between 0 and 5.", nameof(averageRating));
        }

        if (totalRatings < 0)
        {
            throw new ArgumentException("Total ratings cannot be negative.", nameof(totalRatings));
        }

        if (totalCompletedOrders < 0)
        {
            throw new ArgumentException("Total completed orders cannot be negative.", nameof(totalCompletedOrders));
        }

        if (totalCancelledOrders < 0)
        {
            throw new ArgumentException("Total cancelled orders cannot be negative.", nameof(totalCancelledOrders));
        }

        if (totalDisputes < 0)
        {
            throw new ArgumentException("Total disputes cannot be negative.", nameof(totalDisputes));
        }

        if (onTimeShipments < 0)
        {
            throw new ArgumentException("On-time shipments cannot be negative.", nameof(onTimeShipments));
        }

        if (totalShipments < 0)
        {
            throw new ArgumentException("Total shipments cannot be negative.", nameof(totalShipments));
        }

        if (onTimeShipments > totalShipments)
        {
            throw new ArgumentException("On-time shipments cannot exceed total shipments.", nameof(onTimeShipments));
        }

        AverageRating = averageRating;
        TotalRatings = totalRatings;
        TotalCompletedOrders = totalCompletedOrders;
        TotalCancelledOrders = totalCancelledOrders;
        TotalDisputes = totalDisputes;
        OnTimeShipments = onTimeShipments;
        TotalShipments = totalShipments;

        // Calculate rates
        var totalOrders = totalCompletedOrders + totalCancelledOrders;
        CancellationRate = totalOrders > 0
            ? Math.Round((decimal)totalCancelledOrders / totalOrders * 100m, 2)
            : 0m;

        DisputeRate = totalCompletedOrders > 0
            ? Math.Round((decimal)totalDisputes / totalCompletedOrders * 100m, 2)
            : 0m;

        OnTimeShippingRate = totalShipments > 0
            ? Math.Round((decimal)onTimeShipments / totalShipments * 100m, 2)
            : 0m;

        // Calculate overall reputation score
        ReputationScore = CalculateReputationScore();
        UpdatedAt = DateTime.UtcNow;
        LastCalculatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the overall reputation score based on multiple factors.
    /// Formula:
    /// - Average Rating contribution: 40% (scaled from 1-5 to 0-100)
    /// - On-time Shipping Rate: 25%
    /// - Low Cancellation Rate: 20% (inverted - 0% cancellation = 100 points)
    /// - Low Dispute Rate: 15% (inverted - 0% disputes = 100 points)
    /// </summary>
    private decimal CalculateReputationScore()
    {
        // Weight factors (must sum to 1.0)
        const decimal ratingWeight = 0.40m;
        const decimal onTimeShippingWeight = 0.25m;
        const decimal cancellationWeight = 0.20m;
        const decimal disputeWeight = 0.15m;

        // Calculate rating component (scale 1-5 to 0-100)
        // If no ratings yet, use neutral score of 60
        var ratingComponent = TotalRatings > 0
            ? (AverageRating - 1m) / 4m * 100m
            : 60m;

        // On-time shipping component (already 0-100)
        // If no shipments yet, use neutral score of 80
        var onTimeComponent = TotalShipments > 0
            ? OnTimeShippingRate
            : 80m;

        // Cancellation component (inverted - lower is better)
        // Cap at 50% for calculation purposes
        var cancellationComponent = 100m - Math.Min(CancellationRate * 2m, 100m);

        // Dispute component (inverted - lower is better)
        // Cap at 50% for calculation purposes
        var disputeComponent = 100m - Math.Min(DisputeRate * 2m, 100m);

        // Calculate weighted score
        var score =
            (ratingComponent * ratingWeight) +
            (onTimeComponent * onTimeShippingWeight) +
            (cancellationComponent * cancellationWeight) +
            (disputeComponent * disputeWeight);

        // Ensure score is within bounds and round to 2 decimal places
        return Math.Round(Math.Clamp(score, 0m, 100m), 2);
    }

    /// <summary>
    /// Gets a simplified reputation tier for display purposes.
    /// </summary>
    public ReputationTier GetReputationTier()
    {
        return ReputationScore switch
        {
            >= 90m => ReputationTier.Excellent,
            >= 75m => ReputationTier.VeryGood,
            >= 60m => ReputationTier.Good,
            >= 45m => ReputationTier.Fair,
            _ => ReputationTier.NeedsImprovement
        };
    }
}

/// <summary>
/// Simplified reputation tier for display to buyers.
/// </summary>
public enum ReputationTier
{
    /// <summary>Score 90-100: Excellent seller with outstanding performance.</summary>
    Excellent,
    /// <summary>Score 75-89: Very good seller with strong performance.</summary>
    VeryGood,
    /// <summary>Score 60-74: Good seller with satisfactory performance.</summary>
    Good,
    /// <summary>Score 45-59: Fair seller with room for improvement.</summary>
    Fair,
    /// <summary>Score 0-44: Seller needs improvement in key areas.</summary>
    NeedsImprovement
}
