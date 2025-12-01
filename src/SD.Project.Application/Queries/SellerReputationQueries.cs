namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a seller's reputation by store ID.
/// </summary>
public record GetSellerReputationQuery(Guid StoreId);

/// <summary>
/// Query to get simplified reputation summary for buyer display.
/// </summary>
public record GetSellerReputationSummaryQuery(Guid StoreId);

/// <summary>
/// Query to get reputation summaries for multiple stores.
/// </summary>
public record GetSellerReputationSummariesQuery(IEnumerable<Guid> StoreIds);

/// <summary>
/// Query to get top sellers by reputation score.
/// </summary>
public record GetTopSellersByReputationQuery(int Take = 10);
