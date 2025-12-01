namespace SD.Project.ViewModels;

/// <summary>
/// View model for admin marketplace performance dashboard.
/// </summary>
public sealed class DashboardMetricsViewModel
{
    public decimal Gmv { get; init; }
    public int OrderCount { get; init; }
    public int ActiveSellerCount { get; init; }
    public int ActiveProductCount { get; init; }
    public int NewUserCount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool HasData { get; init; }
    public DateTime RefreshedAt { get; init; }

    /// <summary>
    /// Gets the formatted GMV with currency.
    /// </summary>
    public string FormattedGmv => $"{Currency} {Gmv:N2}";

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
            // Include year for both dates when they span different years
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
}

/// <summary>
/// Represents a date range preset for the dashboard filter.
/// </summary>
public sealed class DateRangePreset
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}
