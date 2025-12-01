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

    public async Task<IReadOnlyCollection<string>> GetProductSuggestionsAsync(
        string searchPrefix,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchPrefix))
        {
            return Array.Empty<string>();
        }

        var escapedPrefix = EscapeLikePattern(searchPrefix.Trim());
        var searchPattern = $"%{escapedPrefix}%";
        var results = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active &&
                        EF.Functions.Like(p.Name, searchPattern))
            .Select(p => p.Name)
            .Distinct()
            .Take(maxResults)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Product>> FilterAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        Guid? storeId = null,
        ProductSortOrder sortOrder = ProductSortOrder.Newest,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(searchTerm, category, minPrice, maxPrice, storeId, sortOrder);

        var results = await query.ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyCollection<Product> Items, int TotalCount)> FilterPagedAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        Guid? storeId = null,
        ProductSortOrder sortOrder = ProductSortOrder.Newest,
        int pageNumber = 1,
        int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(searchTerm, category, minPrice, maxPrice, storeId, sortOrder);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        var results = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (results.AsReadOnly(), totalCount);
    }

    private IQueryable<Product> BuildFilterQuery(
        string? searchTerm,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        Guid? storeId,
        ProductSortOrder sortOrder)
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

        // Apply sorting with stable secondary sort by Id
        query = sortOrder switch
        {
            ProductSortOrder.PriceAscending => query.OrderBy(p => p.Price.Amount).ThenBy(p => p.Id),
            ProductSortOrder.PriceDescending => query.OrderByDescending(p => p.Price.Amount).ThenBy(p => p.Id),
            ProductSortOrder.Newest => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        return query;
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

    public async Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetByModerationStatusPagedAsync(
        ProductModerationStatus? moderationStatus = null,
        string? category = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        // Filter by moderation status
        if (moderationStatus.HasValue)
        {
            query = query.Where(p => p.ModerationStatus == moderationStatus.Value);
        }

        // Filter by category (case-insensitive)
        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryLower = category.ToLowerInvariant();
            query = query.Where(p => p.Category.ToLower() == categoryLower);
        }

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchPattern = $"%{searchTerm.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, searchPattern) ||
                EF.Functions.Like(p.Description ?? string.Empty, searchPattern));
        }

        // Exclude archived products from moderation queue
        query = query.Where(p => p.Status != ProductStatus.Archived);

        // Order by created date (newest first) for review queue
        query = query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        var results = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (results.AsReadOnly(), totalCount);
    }

    /// <summary>
    /// Escapes LIKE pattern special characters to prevent SQL injection via wildcards.
    /// </summary>
    private static string EscapeLikePattern(string input)
    {
        return input
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
}
