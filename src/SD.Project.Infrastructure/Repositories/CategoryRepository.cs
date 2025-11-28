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
}
