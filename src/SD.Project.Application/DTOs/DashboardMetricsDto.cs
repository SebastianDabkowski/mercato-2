namespace SD.Project.Application.DTOs;

/// <summary>
/// Dashboard metrics for the admin marketplace performance dashboard.
/// </summary>
/// <param name="Gmv">Total Gross Merchandise Value (order value including shipping).</param>
/// <param name="OrderCount">Total number of orders.</param>
/// <param name="ActiveSellerCount">Number of active sellers.</param>
/// <param name="ActiveProductCount">Number of active products.</param>
/// <param name="NewUserCount">Number of newly registered users.</param>
/// <param name="Currency">Currency code for GMV.</param>
/// <param name="FromDate">Start of the reporting period.</param>
/// <param name="ToDate">End of the reporting period.</param>
/// <param name="HasData">Indicates whether any data was found for the period.</param>
/// <param name="RefreshedAt">Timestamp when the metrics were last refreshed.</param>
public record DashboardMetricsDto(
    decimal Gmv,
    int OrderCount,
    int ActiveSellerCount,
    int ActiveProductCount,
    int NewUserCount,
    string Currency,
    DateTime FromDate,
    DateTime ToDate,
    bool HasData,
    DateTime RefreshedAt);
