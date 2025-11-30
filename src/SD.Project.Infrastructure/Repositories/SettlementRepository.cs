using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of settlement repository.
/// </summary>
public sealed class SettlementRepository : ISettlementRepository
{
    private readonly AppDbContext _context;

    public SettlementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.Settlements
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (settlement is not null)
        {
            await LoadRelatedDataAsync(settlement, cancellationToken);
        }

        return settlement;
    }

    public async Task<Settlement?> GetByStoreAndPeriodAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var settlement = await _context.Settlements
            .Where(s => s.StoreId == storeId && s.Year == year && s.Month == month)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (settlement is not null)
        {
            await LoadRelatedDataAsync(settlement, cancellationToken);
        }

        return settlement;
    }

    public async Task<Settlement?> GetLatestByStoreAndPeriodAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var settlement = await _context.Settlements
            .Where(s => s.StoreId == storeId && s.Year == year && s.Month == month)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (settlement is not null)
        {
            await LoadRelatedDataAsync(settlement, cancellationToken);
        }

        return settlement;
    }

    public async Task<IReadOnlyList<Settlement>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var settlements = await _context.Settlements
            .Where(s => s.StoreId == storeId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenByDescending(s => s.Version)
            .ToListAsync(cancellationToken);

        await LoadRelatedDataForSettlementsAsync(settlements, cancellationToken);

        return settlements;
    }

    public async Task<(IReadOnlyList<Settlement> Settlements, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Settlements
            .Where(s => s.StoreId == storeId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenByDescending(s => s.Version);

        var totalCount = await query.CountAsync(cancellationToken);

        var settlements = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        await LoadRelatedDataForSettlementsAsync(settlements, cancellationToken);

        return (settlements, totalCount);
    }

    public async Task<IReadOnlyList<Settlement>> GetByPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var settlements = await _context.Settlements
            .Where(s => s.Year == year && s.Month == month)
            .OrderBy(s => s.StoreId)
            .ThenByDescending(s => s.Version)
            .ToListAsync(cancellationToken);

        await LoadRelatedDataForSettlementsAsync(settlements, cancellationToken);

        return settlements;
    }

    public async Task<(IReadOnlyList<Settlement> Settlements, int TotalCount)> GetFilteredAsync(
        Guid? storeId,
        int? year,
        int? month,
        SettlementStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Settlements.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(s => s.StoreId == storeId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(s => s.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(s => s.Month == month.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var settlements = await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        await LoadRelatedDataForSettlementsAsync(settlements, cancellationToken);

        return (settlements, totalCount);
    }

    public async Task<int> GetNextVersionAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var maxVersion = await _context.Settlements
            .Where(s => s.StoreId == storeId && s.Year == year && s.Month == month)
            .MaxAsync(s => (int?)s.Version, cancellationToken) ?? 0;

        return maxVersion + 1;
    }

    public async Task<bool> ExistsForPeriodAsync(
        Guid storeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.Settlements
            .AnyAsync(s => s.StoreId == storeId && s.Year == year && s.Month == month, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetStoresWithoutSettlementAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        // Get period boundaries
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // Get store IDs with escrow activity in the period
        var storesWithActivity = await _context.EscrowAllocations
            .Where(a => a.CreatedAt >= periodStart && a.CreatedAt <= periodEnd)
            .Select(a => a.StoreId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get store IDs that already have settlements
        var storesWithSettlement = await _context.Settlements
            .Where(s => s.Year == year && s.Month == month)
            .Select(s => s.StoreId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Return stores with activity but no settlement
        return storesWithActivity.Except(storesWithSettlement).ToList();
    }

    public async Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        await _context.Settlements.AddAsync(settlement, cancellationToken);

        foreach (var item in settlement.Items)
        {
            await _context.SettlementItems.AddAsync(item, cancellationToken);
        }

        foreach (var adjustment in settlement.Adjustments)
        {
            await _context.SettlementAdjustments.AddAsync(adjustment, cancellationToken);
        }
    }

    public async Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        _context.Settlements.Update(settlement);

        // Sync items
        var existingItemIds = (await _context.SettlementItems
            .Where(i => i.SettlementId == settlement.Id)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var item in settlement.Items)
        {
            if (!existingItemIds.Contains(item.Id))
            {
                await _context.SettlementItems.AddAsync(item, cancellationToken);
            }
        }

        // Remove items that are no longer in the settlement
        var currentItemIds = settlement.Items.Select(i => i.Id).ToHashSet();
        var itemsToRemove = await _context.SettlementItems
            .Where(i => i.SettlementId == settlement.Id && !currentItemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);
        _context.SettlementItems.RemoveRange(itemsToRemove);

        // Sync adjustments
        var existingAdjustmentIds = (await _context.SettlementAdjustments
            .Where(a => a.SettlementId == settlement.Id)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var adjustment in settlement.Adjustments)
        {
            if (!existingAdjustmentIds.Contains(adjustment.Id))
            {
                await _context.SettlementAdjustments.AddAsync(adjustment, cancellationToken);
            }
        }

        // Remove adjustments that are no longer in the settlement
        var currentAdjustmentIds = settlement.Adjustments.Select(a => a.Id).ToHashSet();
        var adjustmentsToRemove = await _context.SettlementAdjustments
            .Where(a => a.SettlementId == settlement.Id && !currentAdjustmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        _context.SettlementAdjustments.RemoveRange(adjustmentsToRemove);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task LoadRelatedDataAsync(Settlement settlement, CancellationToken cancellationToken)
    {
        var items = await _context.SettlementItems
            .Where(i => i.SettlementId == settlement.Id)
            .OrderByDescending(i => i.TransactionDate)
            .ToListAsync(cancellationToken);

        settlement.LoadItems(items);

        var adjustments = await _context.SettlementAdjustments
            .Where(a => a.SettlementId == settlement.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        settlement.LoadAdjustments(adjustments);
    }

    private async Task LoadRelatedDataForSettlementsAsync(
        List<Settlement> settlements,
        CancellationToken cancellationToken)
    {
        if (settlements.Count == 0) return;

        var settlementIds = settlements.Select(s => s.Id).ToList();

        var allItems = await _context.SettlementItems
            .Where(i => settlementIds.Contains(i.SettlementId))
            .ToListAsync(cancellationToken);

        var itemsBySettlement = allItems.GroupBy(i => i.SettlementId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allAdjustments = await _context.SettlementAdjustments
            .Where(a => settlementIds.Contains(a.SettlementId))
            .ToListAsync(cancellationToken);

        var adjustmentsBySettlement = allAdjustments.GroupBy(a => a.SettlementId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var settlement in settlements)
        {
            if (itemsBySettlement.TryGetValue(settlement.Id, out var items))
            {
                settlement.LoadItems(items);
            }

            if (adjustmentsBySettlement.TryGetValue(settlement.Id, out var adjustments))
            {
                settlement.LoadAdjustments(adjustments);
            }
        }
    }
}
