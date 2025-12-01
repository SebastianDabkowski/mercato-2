using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed category repository.
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Name.ToLower() == name.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Slug.ToLower() == slug.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var query = _context.Categories
            .AsNoTracking()
            .Where(c => c.Slug.ToLower() == slug.ToLower());

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Category>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var namesList = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.ToLower())
            .Distinct()
            .ToList();

        if (namesList.Count == 0)
        {
            return Array.Empty<Category>();
        }

        var results = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && namesList.Contains(c.Name.ToLower()))
            .ToListAsync(cancellationToken);
        
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Category>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var results = await _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(c => c.ParentId == categoryId, cancellationToken);
    }

    public async Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category is null)
        {
            return 0;
        }

        // Count products that have this category name (using ToLower for case-insensitive comparison)
        var categoryNameLower = category.Name.ToLowerInvariant();
        return await _context.Products
            .Where(p => p.Category.ToLower() == categoryNameLower && p.Status != ProductStatus.Archived)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var categoryIdList = categoryIds.ToList();
        if (categoryIdList.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        // Get all categories with their names
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => categoryIdList.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        // Get product counts by category name (case-insensitive)
        var categoryNames = categories.Select(c => c.Name.ToLowerInvariant()).Distinct().ToList();
        var productCounts = await _context.Products
            .Where(p => categoryNames.Contains(p.Category.ToLower()) && p.Status != ProductStatus.Archived)
            .GroupBy(p => p.Category.ToLower())
            .Select(g => new { CategoryName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var productCountDict = productCounts.ToDictionary(x => x.CategoryName, x => x.Count);

        // Map back to category IDs
        var result = new Dictionary<Guid, int>();
        foreach (var category in categories)
        {
            var nameLower = category.Name.ToLowerInvariant();
            result[category.Id] = productCountDict.GetValueOrDefault(nameLower, 0);
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetChildCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var categoryIdList = categoryIds.ToList();
        if (categoryIdList.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var childCounts = await _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentId.HasValue && categoryIdList.Contains(c.ParentId.Value))
            .GroupBy(c => c.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = categoryIdList.ToDictionary(id => id, _ => 0);
        foreach (var item in childCounts)
        {
            result[item.ParentId] = item.Count;
        }

        return result;
    }

    public async Task<IReadOnlyCollection<Category>> GetSuggestionsAsync(
        string searchPrefix,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchPrefix))
        {
            return Array.Empty<Category>();
        }

        var escapedPrefix = EscapeLikePattern(searchPrefix.Trim());
        var searchPattern = $"%{escapedPrefix}%";
        var results = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && EF.Functions.Like(c.Name, searchPattern))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        _context.Categories.Remove(category);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
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
