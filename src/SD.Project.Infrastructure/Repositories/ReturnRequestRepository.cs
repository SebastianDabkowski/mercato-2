using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of return request persistence.
/// </summary>
public sealed class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly AppDbContext _context;

    public ReturnRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ReturnRequest?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is not null)
        {
            var items = await _context.ReturnRequestItems
                .Where(i => i.ReturnRequestId == id)
                .ToListAsync(cancellationToken);
            request.LoadItems(items);
        }

        return request;
    }

    public async Task<ReturnRequest?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ReturnRequests
            .FirstOrDefaultAsync(r => r.CaseNumber == caseNumber, cancellationToken);
    }

    public async Task<ReturnRequest?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ReturnRequests
            .FirstOrDefaultAsync(r => r.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.ReturnRequests
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            return requests.AsReadOnly();
        }

        // Load all items for all requests in a single query
        var requestIds = requests.Select(r => r.Id).ToList();
        var allItems = await _context.ReturnRequestItems
            .Where(i => requestIds.Contains(i.ReturnRequestId))
            .ToListAsync(cancellationToken);

        // Group items by request ID and load them
        var itemsByRequest = allItems.GroupBy(i => i.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var request in requests)
        {
            if (itemsByRequest.TryGetValue(request.Id, out var items))
            {
                request.LoadItems(items);
            }
        }

        return requests.AsReadOnly();
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.ReturnRequests
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.AsReadOnly();
    }

    public async Task<(IReadOnlyList<ReturnRequest> Requests, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        ReturnRequestStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int skip,
        int take,
        ReturnRequestType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests
            .Where(r => r.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.Type == type.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(r => r.CreatedAt < endOfDay);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (requests.AsReadOnly(), totalCount);
    }

    public async Task<(IReadOnlyList<ReturnRequest> Requests, int TotalCount)> GetFilteredForAdminAsync(
        ReturnRequestStatus? status,
        ReturnRequestType? type,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.CaseNumber.ToLower().Contains(term) ||
                r.Reason.ToLower().Contains(term));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(r => r.CreatedAt < endOfDay);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (requests.AsReadOnly(), totalCount);
    }

    public async Task<bool> ExistsForShipmentAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ReturnRequests
            .AnyAsync(r => r.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task<bool> HasOpenRequestForOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        // An "open" request is one that is not Rejected or Completed
        var openStatuses = new[] { ReturnRequestStatus.Requested, ReturnRequestStatus.Approved };

        // Use a join-based query for better performance
        var query = from item in _context.ReturnRequestItems
                    join request in _context.ReturnRequests on item.ReturnRequestId equals request.Id
                    where item.OrderItemId == orderItemId && openStatuses.Contains(request.Status)
                    select item;

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<ReturnRequest?> GetOpenRequestForOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        var openStatuses = new[] { ReturnRequestStatus.Requested, ReturnRequestStatus.Approved };

        // Use a join-based query for better performance
        var query = from item in _context.ReturnRequestItems
                    join request in _context.ReturnRequests on item.ReturnRequestId equals request.Id
                    where item.OrderItemId == orderItemId && openStatuses.Contains(request.Status)
                    select request;

        var openRequest = await query.FirstOrDefaultAsync(cancellationToken);
        if (openRequest is null)
        {
            return null;
        }

        // Load items for the request
        var items = await _context.ReturnRequestItems
            .Where(i => i.ReturnRequestId == openRequest.Id)
            .ToListAsync(cancellationToken);
        openRequest.LoadItems(items);

        return openRequest;
    }

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        await _context.ReturnRequests.AddAsync(returnRequest, cancellationToken);

        // Also add any items
        foreach (var item in returnRequest.Items)
        {
            await _context.ReturnRequestItems.AddAsync(item, cancellationToken);
        }
    }

    public Task UpdateAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        _context.ReturnRequests.Update(returnRequest);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetCasesPendingSlaBreachCheckAsync(CancellationToken cancellationToken = default)
    {
        // Get cases that:
        // 1. Have SLA tracking enabled (FirstResponseDeadline is set)
        // 2. Are not yet marked as breached
        // 3. Are still pending (Requested or Approved status)
        var pendingStatuses = new[] { ReturnRequestStatus.Requested, ReturnRequestStatus.Approved };

        var cases = await _context.ReturnRequests
            .Where(r => r.FirstResponseDeadline.HasValue
                && !r.SlaBreached
                && pendingStatuses.Contains(r.Status))
            .ToListAsync(cancellationToken);

        return cases.AsReadOnly();
    }

    public async Task<(IReadOnlyList<ReturnRequest> Requests, int TotalCount)> GetSlaBreachedCasesAsync(
        Guid? storeId,
        SlaBreachType? breachType,
        DateTime? fromDate,
        DateTime? toDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests
            .Where(r => r.SlaBreached);

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        if (breachType.HasValue)
        {
            query = query.Where(r => r.SlaBreachType == breachType.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.SlaBreachedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(r => r.SlaBreachedAt < endOfDay);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .OrderByDescending(r => r.SlaBreachedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (requests.AsReadOnly(), totalCount);
    }

    public async Task<(int TotalCases, int CasesWithSla, int CasesResolvedWithinSla, int FirstResponseBreaches, int ResolutionBreaches, TimeSpan AvgFirstResponseTime, TimeSpan AvgResolutionTime)> GetStoreSlaStatisticsAsync(
        Guid storeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var cases = await _context.ReturnRequests
            .Where(r => r.StoreId == storeId && r.CreatedAt >= fromDate && r.CreatedAt < toDate.Date.AddDays(1))
            .ToListAsync(cancellationToken);

        var totalCases = cases.Count;
        var casesWithSla = cases.Count(c => c.HasSlaTracking);
        var casesResolvedWithinSla = cases.Count(c => c.HasSlaTracking && !c.SlaBreached && c.Status == ReturnRequestStatus.Completed);
        var firstResponseBreaches = cases.Count(c => c.SlaBreachType == SlaBreachType.FirstResponse);
        var resolutionBreaches = cases.Count(c => c.SlaBreachType == SlaBreachType.Resolution);

        // Calculate average first response time
        var casesWithResponse = cases.Where(c => c.FirstRespondedAt.HasValue).ToList();
        var avgFirstResponseTime = casesWithResponse.Count > 0
            ? TimeSpan.FromTicks((long)casesWithResponse.Average(c => (c.FirstRespondedAt!.Value - c.CreatedAt).Ticks))
            : TimeSpan.Zero;

        // Calculate average resolution time
        var resolvedCases = cases.Where(c => c.Status == ReturnRequestStatus.Completed && c.CompletedAt.HasValue).ToList();
        var avgResolutionTime = resolvedCases.Count > 0
            ? TimeSpan.FromTicks((long)resolvedCases.Average(c => (c.CompletedAt!.Value - c.CreatedAt).Ticks))
            : TimeSpan.Zero;

        return (totalCases, casesWithSla, casesResolvedWithinSla, firstResponseBreaches, resolutionBreaches, avgFirstResponseTime, avgResolutionTime);
    }

    public async Task<(int TotalCases, int CasesWithSla, int CasesResolvedWithinSla, int TotalBreaches, TimeSpan AvgFirstResponseTime, TimeSpan AvgResolutionTime)> GetAggregateSlaStatisticsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var cases = await _context.ReturnRequests
            .Where(r => r.CreatedAt >= fromDate && r.CreatedAt < toDate.Date.AddDays(1))
            .ToListAsync(cancellationToken);

        var totalCases = cases.Count;
        var casesWithSla = cases.Count(c => c.HasSlaTracking);
        var casesResolvedWithinSla = cases.Count(c => c.HasSlaTracking && !c.SlaBreached && c.Status == ReturnRequestStatus.Completed);
        var totalBreaches = cases.Count(c => c.SlaBreached);

        // Calculate average first response time
        var casesWithResponse = cases.Where(c => c.FirstRespondedAt.HasValue).ToList();
        var avgFirstResponseTime = casesWithResponse.Count > 0
            ? TimeSpan.FromTicks((long)casesWithResponse.Average(c => (c.FirstRespondedAt!.Value - c.CreatedAt).Ticks))
            : TimeSpan.Zero;

        // Calculate average resolution time
        var resolvedCases = cases.Where(c => c.Status == ReturnRequestStatus.Completed && c.CompletedAt.HasValue).ToList();
        var avgResolutionTime = resolvedCases.Count > 0
            ? TimeSpan.FromTicks((long)resolvedCases.Average(c => (c.CompletedAt!.Value - c.CreatedAt).Ticks))
            : TimeSpan.Zero;

        return (totalCases, casesWithSla, casesResolvedWithinSla, totalBreaches, avgFirstResponseTime, avgResolutionTime);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
