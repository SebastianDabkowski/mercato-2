using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of shipping rule repository.
/// </summary>
public sealed class ShippingRuleRepository : IShippingRuleRepository
{
    private readonly AppDbContext _context;

    public ShippingRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShippingRule>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.ShippingRules
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.IsActive)
            .OrderByDescending(r => r.IsDefault)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<ShippingRule?> GetDefaultByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShippingRules
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.IsActive && r.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ShippingRule>> GetDefaultsByStoreIdsAsync(
        IEnumerable<Guid> storeIds,
        CancellationToken cancellationToken = default)
    {
        var storeIdList = storeIds.ToList();
        if (storeIdList.Count == 0)
        {
            return new Dictionary<Guid, ShippingRule>();
        }

        var rules = await _context.ShippingRules
            .AsNoTracking()
            .Where(r => storeIdList.Contains(r.StoreId) && r.IsActive && r.IsDefault)
            .ToListAsync(cancellationToken);

        return rules.ToDictionary(r => r.StoreId);
    }

    public async Task AddAsync(ShippingRule shippingRule, CancellationToken cancellationToken = default)
    {
        await _context.ShippingRules.AddAsync(shippingRule, cancellationToken);
    }

    public Task UpdateAsync(ShippingRule shippingRule, CancellationToken cancellationToken = default)
    {
        _context.ShippingRules.Update(shippingRule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ShippingRule shippingRule, CancellationToken cancellationToken = default)
    {
        _context.ShippingRules.Remove(shippingRule);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
