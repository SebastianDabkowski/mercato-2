namespace SD.Project.Application.Queries;

/// <summary>
/// Query to retrieve seller sales dashboard metrics for a specific period.
/// </summary>
/// <param name="StoreId">The store ID to get metrics for.</param>
/// <param name="FromDate">Start of the reporting period (UTC).</param>
/// <param name="ToDate">End of the reporting period (UTC).</param>
/// <param name="Granularity">Time granularity for the time series (day, week, month).</param>
/// <param name="ProductId">Optional product ID to filter by.</param>
/// <param name="Category">Optional category to filter by.</param>
public record GetSellerSalesDashboardQuery(
    Guid StoreId,
    DateTime FromDate,
    DateTime ToDate,
    string Granularity = "day",
    Guid? ProductId = null,
    string? Category = null);

/// <summary>
/// Query to retrieve filter options for the seller sales dashboard.
/// </summary>
/// <param name="StoreId">The store ID to get filter options for.</param>
public record GetSellerDashboardFilterOptionsQuery(Guid StoreId);
