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

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        await _context.ReturnRequests.AddAsync(returnRequest, cancellationToken);
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
