using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed category attribute repository.
/// </summary>
public sealed class CategoryAttributeRepository : ICategoryAttributeRepository
{
    private readonly AppDbContext _context;

    public CategoryAttributeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryAttributes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CategoryAttribute>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var results = await _context.CategoryAttributes
            .AsNoTracking()
            .Where(a => a.CategoryId == categoryId)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<CategoryAttribute>> GetActiveByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var results = await _context.CategoryAttributes
            .AsNoTracking()
            .Where(a => a.CategoryId == categoryId && !a.IsDeprecated)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<CategoryAttribute>> GetBySharedAttributeIdAsync(Guid sharedAttributeId, CancellationToken cancellationToken = default)
    {
        var results = await _context.CategoryAttributes
            .AsNoTracking()
            .Where(a => a.SharedAttributeId == sharedAttributeId)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<bool> ExistsByNameAsync(Guid categoryId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var query = _context.CategoryAttributes
            .AsNoTracking()
            .Where(a => a.CategoryId == categoryId && a.Name.ToLower() == name.ToLower().Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetCountByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryAttributes
            .CountAsync(a => a.CategoryId == categoryId, cancellationToken);
    }

    public async Task AddAsync(CategoryAttribute attribute, CancellationToken cancellationToken = default)
    {
        await _context.CategoryAttributes.AddAsync(attribute, cancellationToken);
    }

    public void Update(CategoryAttribute attribute)
    {
        _context.CategoryAttributes.Update(attribute);
    }

    public void Delete(CategoryAttribute attribute)
    {
        _context.CategoryAttributes.Remove(attribute);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
