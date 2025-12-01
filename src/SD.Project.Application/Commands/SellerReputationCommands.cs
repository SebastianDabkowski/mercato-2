namespace SD.Project.Application.Commands;

/// <summary>
/// Command to recalculate a seller's reputation score.
/// This fetches the latest activity data and updates the reputation.
/// </summary>
public record RecalculateSellerReputationCommand(Guid StoreId);

/// <summary>
/// Command to batch recalculate reputation scores for stale entries.
/// Used for periodic batch processing of reputation updates.
/// </summary>
public record BatchRecalculateReputationsCommand(
    TimeSpan StaleThreshold,
    int BatchSize = 100);
