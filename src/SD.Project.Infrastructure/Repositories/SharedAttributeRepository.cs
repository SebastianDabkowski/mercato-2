using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed shared attribute repository.
/// </summary>
public sealed class SharedAttributeRepository : ISharedAttributeRepository
{
    private readonly AppDbContext _context;

    public SharedAttributeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SharedAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SharedAttributes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SharedAttribute>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.SharedAttributes
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var query = _context.SharedAttributes
            .AsNoTracking()
            .Where(a => a.Name.ToLower() == name.ToLower().Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetLinkedCategoryCountAsync(Guid sharedAttributeId, CancellationToken cancellationToken = default)
    {
        return await _context.CategoryAttributes
            .CountAsync(a => a.SharedAttributeId == sharedAttributeId, cancellationToken);
    }

    public async Task AddAsync(SharedAttribute attribute, CancellationToken cancellationToken = default)
    {
        await _context.SharedAttributes.AddAsync(attribute, cancellationToken);
    }

    public void Update(SharedAttribute attribute)
    {
        _context.SharedAttributes.Update(attribute);
    }

    public void Delete(SharedAttribute attribute)
    {
        _context.SharedAttributes.Remove(attribute);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
