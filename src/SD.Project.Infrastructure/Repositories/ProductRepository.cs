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
        var results = await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive && p.Status == ProductStatus.Active)
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

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
