using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the escrow repository.
/// </summary>
public sealed class EscrowRepository : IEscrowRepository
{
    private readonly AppDbContext _context;

    public EscrowRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EscrowPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var escrow = await _context.EscrowPayments
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (escrow is not null)
        {
            var allocations = await _context.EscrowAllocations
                .Where(a => a.EscrowPaymentId == id)
                .ToListAsync(cancellationToken);
            escrow.LoadAllocations(allocations);
        }

        return escrow;
    }

    public async Task<EscrowPayment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var escrow = await _context.EscrowPayments
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);

        if (escrow is not null)
        {
            var allocations = await _context.EscrowAllocations
                .Where(a => a.EscrowPaymentId == escrow.Id)
                .ToListAsync(cancellationToken);
            escrow.LoadAllocations(allocations);
        }

        return escrow;
    }

    public async Task<IReadOnlyList<EscrowPayment>> GetByBuyerIdAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default)
    {
        var escrows = await _context.EscrowPayments
            .Where(e => e.BuyerId == buyerId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        if (escrows.Count == 0)
        {
            return escrows;
        }

        // Fetch all allocations for these escrows in a single query to avoid N+1
        var escrowIds = escrows.Select(e => e.Id).ToList();
        var allAllocations = await _context.EscrowAllocations
            .Where(a => escrowIds.Contains(a.EscrowPaymentId))
            .ToListAsync(cancellationToken);

        // Group allocations by escrow ID and load them
        var allocationsByEscrow = allAllocations.ToLookup(a => a.EscrowPaymentId);
        foreach (var escrow in escrows)
        {
            escrow.LoadAllocations(allocationsByEscrow[escrow.Id]);
        }

        return escrows;
    }

    public async Task<IReadOnlyList<EscrowAllocation>> GetAllocationsByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .Where(a => a.StoreId == storeId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EscrowAllocation?> GetAllocationByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .FirstOrDefaultAsync(a => a.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task<EscrowAllocation?> GetAllocationByIdAsync(
        Guid allocationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);
    }

    public async Task<IReadOnlyList<EscrowAllocation>> GetEligibleForPayoutAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .Where(a => a.Status == EscrowAllocationStatus.Held && a.IsEligibleForPayout)
            .OrderBy(a => a.PayoutEligibleAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscrowAllocation>> GetEligibleForPayoutByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .Where(a => a.StoreId == storeId &&
                        a.Status == EscrowAllocationStatus.Held &&
                        a.IsEligibleForPayout)
            .OrderBy(a => a.PayoutEligibleAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscrowAllocation>> GetHeldAllocationsByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .Where(a => a.StoreId == storeId && a.Status == EscrowAllocationStatus.Held)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<EscrowAllocation> Allocations, int TotalCount)> GetReleasedAllocationsByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EscrowAllocations
            .Where(a => a.StoreId == storeId && a.Status == EscrowAllocationStatus.Released);

        var totalCount = await query.CountAsync(cancellationToken);

        var allocations = await query
            .OrderByDescending(a => a.ReleasedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (allocations, totalCount);
    }

    public async Task AddAsync(EscrowPayment escrowPayment, CancellationToken cancellationToken = default)
    {
        await _context.EscrowPayments.AddAsync(escrowPayment, cancellationToken);

        foreach (var allocation in escrowPayment.Allocations)
        {
            await _context.EscrowAllocations.AddAsync(allocation, cancellationToken);
        }
    }

    public Task UpdateAsync(EscrowPayment escrowPayment, CancellationToken cancellationToken = default)
    {
        _context.EscrowPayments.Update(escrowPayment);

        foreach (var allocation in escrowPayment.Allocations)
        {
            _context.EscrowAllocations.Update(allocation);
        }

        return Task.CompletedTask;
    }

    public async Task AddLedgerEntryAsync(EscrowLedger ledgerEntry, CancellationToken cancellationToken = default)
    {
        await _context.EscrowLedgers.AddAsync(ledgerEntry, cancellationToken);
    }

    public async Task<IReadOnlyList<EscrowLedger>> GetLedgerEntriesAsync(
        Guid escrowPaymentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowLedgers
            .Where(l => l.EscrowPaymentId == escrowPaymentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<EscrowLedger> Entries, int TotalCount)> GetLedgerEntriesByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EscrowLedgers
            .Where(l => l.StoreId == storeId);

        var totalCount = await query.CountAsync(cancellationToken);

        var entries = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (entries, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscrowAllocation>> GetAllocationsByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .Where(a => a.CreatedAt >= fromDate && a.CreatedAt <= toDate)
            .OrderBy(a => a.StoreId)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscrowAllocation>> GetAllocationsByStoreIdAndDateRangeAsync(
        Guid storeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.EscrowAllocations
            .Where(a => a.StoreId == storeId && a.CreatedAt >= fromDate && a.CreatedAt <= toDate)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
