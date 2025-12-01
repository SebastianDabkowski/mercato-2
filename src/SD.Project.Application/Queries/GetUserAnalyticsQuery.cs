namespace SD.Project.Application.Queries;

/// <summary>
/// Query to retrieve user analytics metrics for a specific period.
/// </summary>
/// <param name="FromDate">Start of the reporting period (UTC).</param>
/// <param name="ToDate">End of the reporting period (UTC).</param>
public record GetUserAnalyticsQuery(DateTime FromDate, DateTime ToDate);
