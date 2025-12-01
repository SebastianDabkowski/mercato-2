using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for querying analytics events.
/// Provides read-only access to recorded events for Phase 2 analytics dashboards.
/// </summary>
public sealed class AnalyticsQueryService
{
    private readonly IAnalyticsEventRepository _repository;

    public AnalyticsQueryService(IAnalyticsEventRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    /// Gets analytics events within a time range with optional filtering and pagination.
    /// </summary>
    public async Task<AnalyticsEventsPagedResultDto> HandleAsync(
        GetAnalyticsEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddSeconds(-1);

        var (events, totalCount) = await _repository.GetByTimeRangeAsync(
            fromDate,
            toDate,
            query.EventType,
            skip,
            pageSize,
            cancellationToken);

        var eventDtos = events.Select(MapToDto).ToList().AsReadOnly();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new AnalyticsEventsPagedResultDto(
            eventDtos,
            totalCount,
            pageNumber,
            pageSize,
            totalPages);
    }

    /// <summary>
    /// Gets a summary of event counts by type for a time range.
    /// </summary>
    public async Task<AnalyticsEventSummaryDto> HandleAsync(
        GetAnalyticsEventSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddSeconds(-1);

        var counts = await _repository.GetEventCountsByTypeAsync(fromDate, toDate, cancellationToken);

        return new AnalyticsEventSummaryDto(
            counts.GetValueOrDefault(AnalyticsEventType.Search, 0),
            counts.GetValueOrDefault(AnalyticsEventType.ProductView, 0),
            counts.GetValueOrDefault(AnalyticsEventType.AddToCart, 0),
            counts.GetValueOrDefault(AnalyticsEventType.CheckoutStart, 0),
            counts.GetValueOrDefault(AnalyticsEventType.OrderComplete, 0),
            query.FromDate.Date,
            query.ToDate.Date);
    }

    private static AnalyticsEventDto MapToDto(AnalyticsEvent e)
    {
        return new AnalyticsEventDto(
            e.Id,
            e.Timestamp,
            e.EventType.ToString(),
            e.UserId,
            e.SessionId,
            e.ProductId,
            e.SellerId,
            e.OrderId,
            e.SearchTerm,
            e.Quantity,
            e.Value,
            e.Currency);
    }
}
