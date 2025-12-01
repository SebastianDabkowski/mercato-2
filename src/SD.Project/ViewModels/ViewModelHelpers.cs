namespace SD.Project.ViewModels;

/// <summary>
/// Helper methods for formatting display values in view models.
/// </summary>
public static class ViewModelHelpers
{
    /// <summary>
    /// Formats a date range as a human-readable label.
    /// </summary>
    /// <param name="fromDate">Start date of the range.</param>
    /// <param name="toDate">End date of the range.</param>
    /// <returns>A formatted date range string.</returns>
    public static string FormatPeriodLabel(DateTime fromDate, DateTime toDate)
    {
        if (fromDate.Date == toDate.Date)
        {
            return $"{fromDate:MMM dd, yyyy}";
        }
        // Include year for both dates when they span different years
        if (fromDate.Year != toDate.Year)
        {
            return $"{fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}";
        }
        return $"{fromDate:MMM dd} - {toDate:MMM dd, yyyy}";
    }

    /// <summary>
    /// Formats a refresh timestamp as a human-readable string.
    /// </summary>
    /// <param name="refreshedAt">The UTC timestamp when data was refreshed.</param>
    /// <returns>A formatted refresh time string.</returns>
    public static string FormatRefreshTime(DateTime refreshedAt)
    {
        return $"Last updated: {refreshedAt:HH:mm:ss} UTC";
    }
}
