namespace SD.Project.Application.DTOs;

/// <summary>
/// Seller sales dashboard metrics DTO.
/// </summary>
/// <param name="Gmv">Total Gross Merchandise Value (order value including shipping).</param>
/// <param name="OrderCount">Total number of orders.</param>
/// <param name="ItemCount">Total number of items sold.</param>
/// <param name="AverageOrderValue">Average order value.</param>
/// <param name="Currency">Currency code for monetary values.</param>
/// <param name="FromDate">Start of the reporting period.</param>
/// <param name="ToDate">End of the reporting period.</param>
/// <param name="HasData">Indicates whether any data was found for the period.</param>
/// <param name="RefreshedAt">Timestamp when the metrics were last refreshed.</param>
/// <param name="TimeSeries">Time-series data points for charting.</param>
/// <param name="Granularity">The time granularity used for the time series.</param>
public record SellerSalesDashboardDto(
    decimal Gmv,
    int OrderCount,
    int ItemCount,
    decimal AverageOrderValue,
    string Currency,
    DateTime FromDate,
    DateTime ToDate,
    bool HasData,
    DateTime RefreshedAt,
    IReadOnlyList<SellerSalesDataPointDto> TimeSeries,
    string Granularity);

/// <summary>
/// A single data point in a time-series of sales data.
/// </summary>
/// <param name="PeriodStart">Start of the period.</param>
/// <param name="PeriodLabel">Human-readable label for the period.</param>
/// <param name="Gmv">GMV for this period.</param>
/// <param name="OrderCount">Order count for this period.</param>
public record SellerSalesDataPointDto(
    DateTime PeriodStart,
    string PeriodLabel,
    decimal Gmv,
    int OrderCount);

/// <summary>
/// Filter options for the seller sales dashboard.
/// </summary>
/// <param name="Products">List of products with sales available for filtering.</param>
/// <param name="Categories">List of categories with sales available for filtering.</param>
public record SellerDashboardFilterOptionsDto(
    IReadOnlyList<ProductFilterOptionDto> Products,
    IReadOnlyList<string> Categories);

/// <summary>
/// A product option for filtering sales data.
/// </summary>
/// <param name="ProductId">The product ID.</param>
/// <param name="ProductName">The product name.</param>
public record ProductFilterOptionDto(
    Guid ProductId,
    string ProductName);
