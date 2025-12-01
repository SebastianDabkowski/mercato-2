using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to retrieve analytics events within a time range.
/// </summary>
/// <param name="FromDate">Start of the time range.</param>
/// <param name="ToDate">End of the time range.</param>
/// <param name="EventType">Optional filter by event type.</param>
/// <param name="PageNumber">Page number for pagination (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
public record GetAnalyticsEventsQuery(
    DateTime FromDate,
    DateTime ToDate,
    AnalyticsEventType? EventType = null,
    int PageNumber = 1,
    int PageSize = 50);

/// <summary>
/// Query to retrieve analytics event count summary.
/// </summary>
/// <param name="FromDate">Start of the time range.</param>
/// <param name="ToDate">End of the time range.</param>
public record GetAnalyticsEventSummaryQuery(
    DateTime FromDate,
    DateTime ToDate);
