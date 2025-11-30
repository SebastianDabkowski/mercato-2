namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a specific payout by ID.
/// </summary>
public sealed record GetPayoutByIdQuery(Guid PayoutId);

/// <summary>
/// Query to get payouts for a store with pagination.
/// </summary>
public sealed record GetPayoutsByStoreIdQuery(
    Guid StoreId,
    int Skip = 0,
    int Take = 20);

/// <summary>
/// Query to get payout summary for a seller.
/// </summary>
public sealed record GetPayoutSummaryQuery(Guid StoreId);

/// <summary>
/// Query to get payouts by status.
/// </summary>
public sealed record GetPayoutsByStatusQuery(string Status);
