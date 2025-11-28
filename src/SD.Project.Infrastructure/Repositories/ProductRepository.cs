using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status != ProductStatus.Archived)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var results = await _context.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<Product?> GetBySkuAndStoreIdAsync(string sku, Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Sku == sku && p.StoreId == storeId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetBySkusAndStoreIdAsync(IEnumerable<string> skus, Guid storeId, CancellationToken cancellationToken = default)
    {
        var skuList = skus.ToList();
        var results = await _context.Products
            .Where(p => p.Sku != null && skuList.Contains(p.Sku) && p.StoreId == storeId)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddRangeAsync(products, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
