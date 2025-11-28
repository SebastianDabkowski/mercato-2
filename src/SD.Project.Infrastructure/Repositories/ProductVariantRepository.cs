using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for product variants.
/// </summary>
public sealed class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _context;

    public ProductVariantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var results = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<ProductVariant>> GetAvailableByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var results = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId && v.IsAvailable && v.Stock > 0)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<ProductVariant?> GetBySkuAndProductIdAsync(string sku, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Sku == sku && v.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariants.AddAsync(variant, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ProductVariant> variants, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariants.AddRangeAsync(variants, cancellationToken);
    }

    public void Update(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public void Delete(ProductVariant variant)
    {
        _context.ProductVariants.Remove(variant);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
