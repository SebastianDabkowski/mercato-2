namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for seller-specific sales dashboard metrics.
/// </summary>
public interface ISellerDashboardRepository
{
    /// <summary>
    /// Gets sales metrics for a specific seller/store over a date range.
    /// </summary>
    /// <param name="storeId">The store ID to get metrics for.</param>
    /// <param name="fromDate">Start of the reporting period (UTC).</param>
    /// <param name="toDate">End of the reporting period (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Seller sales metrics.</returns>
    Task<SellerSalesMetrics> GetSalesMetricsAsync(
        Guid storeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets time-series sales data for a specific seller/store over a date range.
    /// </summary>
    /// <param name="storeId">The store ID to get data for.</param>
    /// <param name="fromDate">Start of the reporting period (UTC).</param>
    /// <param name="toDate">End of the reporting period (UTC).</param>
    /// <param name="granularity">Time granularity for the data points.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of time-series data points.</returns>
    Task<IReadOnlyList<SellerSalesDataPoint>> GetSalesTimeSeriesAsync(
        Guid storeId,
        DateTime fromDate,
        DateTime toDate,
        TimeGranularity granularity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sales metrics filtered by product for a specific seller/store.
    /// </summary>
    /// <param name="storeId">The store ID to get metrics for.</param>
    /// <param name="productId">Optional product ID to filter by.</param>
    /// <param name="category">Optional category name to filter by.</param>
    /// <param name="fromDate">Start of the reporting period (UTC).</param>
    /// <param name="toDate">End of the reporting period (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered seller sales metrics.</returns>
    Task<SellerSalesMetrics> GetFilteredSalesMetricsAsync(
        Guid storeId,
        Guid? productId,
        string? category,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of products for a seller that have sales data.
    /// Used for populating product filter dropdown.
    /// </summary>
    /// <param name="storeId">The store ID to get products for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products with sales.</returns>
    Task<IReadOnlyList<SellerProductFilterOption>> GetProductsWithSalesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of categories for a seller that have sales data.
    /// Used for populating category filter dropdown.
    /// </summary>
    /// <param name="storeId">The store ID to get categories for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of categories with sales.</returns>
    Task<IReadOnlyList<string>> GetCategoriesWithSalesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Time granularity for sales data aggregation.
/// </summary>
public enum TimeGranularity
{
    /// <summary>Daily aggregation.</summary>
    Day,
    /// <summary>Weekly aggregation.</summary>
    Week,
    /// <summary>Monthly aggregation.</summary>
    Month
}

/// <summary>
/// Aggregate sales metrics for a seller.
/// </summary>
/// <param name="Gmv">Total Gross Merchandise Value (order value including shipping).</param>
/// <param name="OrderCount">Total number of orders.</param>
/// <param name="ItemCount">Total number of items sold.</param>
/// <param name="AverageOrderValue">Average order value.</param>
public record SellerSalesMetrics(
    decimal Gmv,
    int OrderCount,
    int ItemCount,
    decimal AverageOrderValue);

/// <summary>
/// A single data point in a time-series of sales data.
/// </summary>
/// <param name="PeriodStart">Start of the period.</param>
/// <param name="PeriodLabel">Human-readable label for the period.</param>
/// <param name="Gmv">GMV for this period.</param>
/// <param name="OrderCount">Order count for this period.</param>
public record SellerSalesDataPoint(
    DateTime PeriodStart,
    string PeriodLabel,
    decimal Gmv,
    int OrderCount);

/// <summary>
/// A product option for filtering sales data.
/// </summary>
/// <param name="ProductId">The product ID.</param>
/// <param name="ProductName">The product name.</param>
public record SellerProductFilterOption(
    Guid ProductId,
    string ProductName);
