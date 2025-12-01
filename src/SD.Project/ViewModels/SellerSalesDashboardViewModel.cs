namespace SD.Project.ViewModels;

/// <summary>
/// View model for seller sales dashboard.
/// </summary>
public sealed class SellerSalesDashboardViewModel
{
    public decimal Gmv { get; init; }
    public int OrderCount { get; init; }
    public int ItemCount { get; init; }
    public decimal AverageOrderValue { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool HasData { get; init; }
    public DateTime RefreshedAt { get; init; }
    public string Granularity { get; init; } = "day";
    public IReadOnlyList<SellerSalesDataPointViewModel> TimeSeries { get; init; } = Array.Empty<SellerSalesDataPointViewModel>();

    /// <summary>
    /// Gets the formatted GMV with currency.
    /// </summary>
    public string FormattedGmv => $"{Currency} {Gmv:N2}";

    /// <summary>
    /// Gets the formatted average order value with currency.
    /// </summary>
    public string FormattedAverageOrderValue => $"{Currency} {AverageOrderValue:N2}";

    /// <summary>
    /// Gets the period display label.
    /// </summary>
    public string PeriodLabel
    {
        get
        {
            if (FromDate.Date == ToDate.Date)
            {
                return $"{FromDate:MMM dd, yyyy}";
            }
            if (FromDate.Year != ToDate.Year)
            {
                return $"{FromDate:MMM dd, yyyy} - {ToDate:MMM dd, yyyy}";
            }
            return $"{FromDate:MMM dd} - {ToDate:MMM dd, yyyy}";
        }
    }

    /// <summary>
    /// Gets the last refresh time in a human-readable format.
    /// </summary>
    public string RefreshTimeDisplay => $"Last updated: {RefreshedAt:HH:mm:ss} UTC";

    /// <summary>
    /// Gets the time series data as JSON for charting.
    /// </summary>
    public string TimeSeriesLabelsJson => System.Text.Json.JsonSerializer.Serialize(
        TimeSeries.Select(dp => dp.PeriodLabel).ToArray());

    /// <summary>
    /// Gets the GMV values as JSON for charting.
    /// </summary>
    public string TimeSeriesGmvJson => System.Text.Json.JsonSerializer.Serialize(
        TimeSeries.Select(dp => dp.Gmv).ToArray());

    /// <summary>
    /// Gets the order count values as JSON for charting.
    /// </summary>
    public string TimeSeriesOrderCountJson => System.Text.Json.JsonSerializer.Serialize(
        TimeSeries.Select(dp => dp.OrderCount).ToArray());
}

/// <summary>
/// A single data point in a time-series of sales data for display.
/// </summary>
public sealed class SellerSalesDataPointViewModel
{
    public DateTime PeriodStart { get; init; }
    public string PeriodLabel { get; init; } = string.Empty;
    public decimal Gmv { get; init; }
    public int OrderCount { get; init; }
}

/// <summary>
/// Represents a granularity option for the dashboard filter.
/// </summary>
public sealed class GranularityOption
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

/// <summary>
/// Represents a product filter option for the dashboard.
/// </summary>
public sealed class ProductFilterOption
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}
