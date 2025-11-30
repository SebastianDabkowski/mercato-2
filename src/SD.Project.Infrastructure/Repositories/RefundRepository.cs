using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of refund repository.
/// </summary>
public sealed class RefundRepository : IRefundRepository
{
    private readonly AppDbContext _context;

    public RefundRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Refund>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .Where(r => r.ShipmentId == shipmentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Refund> Refunds, int TotalCount)> GetByStatusAsync(
        RefundStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Refunds
            .AsNoTracking()
            .Where(r => r.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var refunds = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (refunds, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Refund> Refunds, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        RefundStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Refunds
            .AsNoTracking()
            .Where(r => r.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var refunds = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (refunds, totalCount);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalRefundedAmountAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == RefundStatus.Completed)
            .SumAsync(r => r.Amount, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await _context.Refunds.AddAsync(refund, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        _context.Refunds.Update(refund);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
