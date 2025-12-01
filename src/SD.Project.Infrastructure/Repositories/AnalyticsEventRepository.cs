using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the analytics event repository.
/// </summary>
public sealed class AnalyticsEventRepository : IAnalyticsEventRepository
{
    private readonly AppDbContext _context;

    public AnalyticsEventRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analyticsEvent);
        await _context.AnalyticsEvents.AddAsync(analyticsEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AnalyticsEvent> Events, int TotalCount)> GetByTimeRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        AnalyticsEventType? eventType = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate);

        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (events.AsReadOnly(), totalCount);
    }

    /// <inheritdoc />
    public async Task<Dictionary<AnalyticsEventType, int>> GetEventCountsByTypeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(
                x => x.EventType,
                x => x.Count,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
