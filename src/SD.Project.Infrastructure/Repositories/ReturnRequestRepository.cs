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
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests
            .Where(r => r.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
