using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of shipping method persistence.
/// </summary>
public sealed class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly AppDbContext _context;

    public ShippingMethodRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingMethod>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingMethods
            .Where(s => s.StoreId == storeId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.BaseCost)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ShippingMethod>>> GetByStoreIdsAsync(
        IEnumerable<Guid> storeIds,
        CancellationToken cancellationToken = default)
    {
        var storeIdList = storeIds.ToList();
        var methods = await _context.ShippingMethods
            .Where(s => s.StoreId.HasValue && storeIdList.Contains(s.StoreId.Value) && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.BaseCost)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return methods
            .GroupBy(s => s.StoreId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ShippingMethod>)g.ToList().AsReadOnly());
    }

    public async Task<ShippingMethod?> GetDefaultByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingMethods
            .Where(s => s.StoreId == storeId && s.IsActive && s.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingMethod>> GetPlatformMethodsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ShippingMethods
            .Where(s => s.StoreId == null && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.BaseCost)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken = default)
    {
        await _context.ShippingMethods.AddAsync(shippingMethod, cancellationToken);
    }

    public Task UpdateAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken = default)
    {
        _context.ShippingMethods.Update(shippingMethod);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
