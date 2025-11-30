using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of seller payout repository.
/// </summary>
public sealed class SellerPayoutRepository : ISellerPayoutRepository
{
    private readonly AppDbContext _context;

    public SellerPayoutRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SellerPayout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payout = await _context.SellerPayouts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payout is not null)
        {
            var items = await _context.SellerPayoutItems
                .Where(i => i.SellerPayoutId == id)
                .ToListAsync(cancellationToken);

            payout.LoadItems(items);
        }

        return payout;
    }

    public async Task<IReadOnlyList<SellerPayout>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var payouts = await _context.SellerPayouts
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        await LoadItemsForPayoutsAsync(payouts, cancellationToken);

        return payouts;
    }

    public async Task<(IReadOnlyList<SellerPayout> Payouts, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SellerPayouts
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var payouts = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        await LoadItemsForPayoutsAsync(payouts, cancellationToken);

        return (payouts, totalCount);
    }

    public async Task<(IReadOnlyList<SellerPayout> Payouts, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        SellerPayoutStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SellerPayouts
            .Where(p => p.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.ScheduledDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Include the entire end date
            var endDate = toDate.Value.Date.AddDays(1);
            query = query.Where(p => p.ScheduledDate < endDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var payouts = await query
            .OrderByDescending(p => p.ScheduledDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        await LoadItemsForPayoutsAsync(payouts, cancellationToken);

        return (payouts, totalCount);
    }

    public async Task<IReadOnlyList<SellerPayout>> GetByStatusAsync(
        SellerPayoutStatus status,
        CancellationToken cancellationToken = default)
    {
        var payouts = await _context.SellerPayouts
            .Where(p => p.Status == status)
            .OrderBy(p => p.ScheduledDate)
            .ToListAsync(cancellationToken);

        await LoadItemsForPayoutsAsync(payouts, cancellationToken);

        return payouts;
    }

    public async Task<IReadOnlyList<SellerPayout>> GetScheduledForProcessingAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default)
    {
        var payouts = await _context.SellerPayouts
            .Where(p => p.Status == SellerPayoutStatus.Scheduled && p.ScheduledDate < beforeDate)
            .OrderBy(p => p.ScheduledDate)
            .ToListAsync(cancellationToken);

        await LoadItemsForPayoutsAsync(payouts, cancellationToken);

        return payouts;
    }

    public async Task<IReadOnlyList<SellerPayout>> GetDueForRetryAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        var payouts = await _context.SellerPayouts
            .Where(p => p.Status == SellerPayoutStatus.Failed &&
                        p.NextRetryAt.HasValue &&
                        p.NextRetryAt.Value <= asOfDate &&
                        p.RetryCount < p.MaxRetries)
            .OrderBy(p => p.NextRetryAt)
            .ToListAsync(cancellationToken);

        await LoadItemsForPayoutsAsync(payouts, cancellationToken);

        return payouts;
    }

    public async Task<SellerPayout?> GetCurrentScheduledPayoutAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var payout = await _context.SellerPayouts
            .Where(p => p.StoreId == storeId && p.Status == SellerPayoutStatus.Scheduled)
            .OrderByDescending(p => p.ScheduledDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (payout is not null)
        {
            var items = await _context.SellerPayoutItems
                .Where(i => i.SellerPayoutId == payout.Id)
                .ToListAsync(cancellationToken);

            payout.LoadItems(items);
        }

        return payout;
    }

    public async Task<bool> IsAllocationInPayoutAsync(
        Guid escrowAllocationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SellerPayoutItems
            .AnyAsync(i => i.EscrowAllocationId == escrowAllocationId, cancellationToken);
    }

    public async Task AddAsync(SellerPayout payout, CancellationToken cancellationToken = default)
    {
        await _context.SellerPayouts.AddAsync(payout, cancellationToken);

        foreach (var item in payout.Items)
        {
            await _context.SellerPayoutItems.AddAsync(item, cancellationToken);
        }
    }

    public async Task UpdateAsync(SellerPayout payout, CancellationToken cancellationToken = default)
    {
        _context.SellerPayouts.Update(payout);

        // Add any new items
        var existingItemIds = (await _context.SellerPayoutItems
            .Where(i => i.SellerPayoutId == payout.Id)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var item in payout.Items)
        {
            if (!existingItemIds.Contains(item.Id))
            {
                await _context.SellerPayoutItems.AddAsync(item, cancellationToken);
            }
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task LoadItemsForPayoutsAsync(
        List<SellerPayout> payouts,
        CancellationToken cancellationToken)
    {
        if (payouts.Count == 0) return;

        var payoutIds = payouts.Select(p => p.Id).ToList();
        var allItems = await _context.SellerPayoutItems
            .Where(i => payoutIds.Contains(i.SellerPayoutId))
            .ToListAsync(cancellationToken);

        var itemsByPayout = allItems.GroupBy(i => i.SellerPayoutId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var payout in payouts)
        {
            if (itemsByPayout.TryGetValue(payout.Id, out var items))
            {
                payout.LoadItems(items);
            }
        }
    }
}
