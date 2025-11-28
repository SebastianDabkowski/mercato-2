using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of store repository.
/// </summary>
public sealed class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Store?> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.Stores
            .FirstOrDefaultAsync(s => s.SellerId == sellerId, cancellationToken);
    }

    public async Task<Store?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return await _context.Stores
            .FirstOrDefaultAsync(s => EF.Functions.Like(s.Name, normalizedName), cancellationToken);
    }

    public async Task<Store?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await _context.Stores
            .FirstOrDefaultAsync(s => s.Slug == normalizedSlug, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeStoreId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var query = _context.Stores.Where(s => EF.Functions.Like(s.Name, normalizedName));

        if (excludeStoreId.HasValue)
        {
            query = query.Where(s => s.Id != excludeStoreId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeStoreId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var query = _context.Stores.Where(s => s.Slug == normalizedSlug);

        if (excludeStoreId.HasValue)
        {
            query = query.Where(s => s.Id != excludeStoreId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Store store, CancellationToken cancellationToken = default)
    {
        await _context.Stores.AddAsync(store, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Store>> GetPubliclyVisibleAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.Stores
            .AsNoTracking()
            .Where(s => s.Status == StoreStatus.Active || s.Status == StoreStatus.LimitedActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Store>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var results = await _context.Stores
            .AsNoTracking()
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
