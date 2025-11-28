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

    public async Task<IReadOnlyCollection<Product>> GetByCategoryAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        var categoryNameLower = categoryName.ToLowerInvariant();
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.Category.ToLower() == categoryNameLower && p.Status == ProductStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<Product>();
        }

        var searchPattern = $"%{searchTerm.Trim()}%";
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active &&
                        (EF.Functions.Like(p.Name, searchPattern) ||
                         EF.Functions.Like(p.Description ?? string.Empty, searchPattern)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> FilterAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        Guid? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        // Apply search term filter (name and description)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchPattern = $"%{searchTerm.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, searchPattern) ||
                EF.Functions.Like(p.Description ?? string.Empty, searchPattern));
        }

        // Apply category filter (case-insensitive)
        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryLower = category.ToLowerInvariant();
            query = query.Where(p => p.Category.ToLower() == categoryLower);
        }

        // Apply price range filters
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount <= maxPrice.Value);
        }

        // For public views, always show only active products
        // The condition filter is ignored for public views to prevent exposing non-public products
        query = query.Where(p => p.Status == ProductStatus.Active);

        // Apply store/seller filter
        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId.Value);
        }

        var results = await query
            .OrderByDescending(p => p.CreatedAt)
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
