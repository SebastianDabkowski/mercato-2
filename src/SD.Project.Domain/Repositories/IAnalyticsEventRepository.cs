using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for analytics events.
/// Supports write operations for event recording and read operations for basic querying.
/// </summary>
public interface IAnalyticsEventRepository
{
    /// <summary>
    /// Records a new analytics event.
    /// </summary>
    /// <param name="analyticsEvent">The event to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events within a time range, optionally filtered by event type.
    /// </summary>
    /// <param name="fromDate">Start of the time range (inclusive).</param>
    /// <param name="toDate">End of the time range (inclusive).</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the events and total count.</returns>
    Task<(IReadOnlyList<AnalyticsEvent> Events, int TotalCount)> GetByTimeRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        AnalyticsEventType? eventType = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of events by type within a time range.
    /// </summary>
    /// <param name="fromDate">Start of the time range (inclusive).</param>
    /// <param name="toDate">End of the time range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping event types to their counts.</returns>
    Task<Dictionary<AnalyticsEventType, int>> GetEventCountsByTypeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
