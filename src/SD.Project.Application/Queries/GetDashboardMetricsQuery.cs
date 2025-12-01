namespace SD.Project.Application.Queries;

/// <summary>
/// Query to retrieve admin dashboard metrics for a specific period.
/// </summary>
/// <param name="FromDate">Start of the reporting period (UTC).</param>
/// <param name="ToDate">End of the reporting period (UTC).</param>
public record GetDashboardMetricsQuery(DateTime FromDate, DateTime ToDate);
