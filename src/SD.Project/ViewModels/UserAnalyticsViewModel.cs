namespace SD.Project.ViewModels;

/// <summary>
/// View model for user registration and activity analytics.
/// All data is aggregated and anonymised for privacy compliance.
/// </summary>
public sealed class UserAnalyticsViewModel
{
    public int NewBuyerCount { get; init; }
    public int NewSellerCount { get; init; }
    public int ActiveUserCount { get; init; }
    public int UsersWithOrdersCount { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool HasData { get; init; }
    public DateTime RefreshedAt { get; init; }

    /// <summary>
    /// Gets the total number of new registrations (buyers + sellers).
    /// </summary>
    public int TotalNewRegistrations => NewBuyerCount + NewSellerCount;

    /// <summary>
    /// Gets the period display label.
    /// </summary>
    public string PeriodLabel => ViewModelHelpers.FormatPeriodLabel(FromDate, ToDate);

    /// <summary>
    /// Gets the last refresh time in a human-readable format.
    /// </summary>
    public string RefreshTimeDisplay => ViewModelHelpers.FormatRefreshTime(RefreshedAt);
}
