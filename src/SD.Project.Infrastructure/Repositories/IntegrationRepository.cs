using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for integrations.
/// </summary>
public sealed class IntegrationRepository : IIntegrationRepository
{
    private readonly AppDbContext _context;

    public IntegrationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Integrations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Integration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.Integrations
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Integration>> GetByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default)
    {
        var results = await _context.Integrations
            .AsNoTracking()
            .Where(i => i.Type == type)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Integration>> GetByEnvironmentAsync(IntegrationEnvironment environment, CancellationToken cancellationToken = default)
    {
        var results = await _context.Integrations
            .AsNoTracking()
            .Where(i => i.Environment == environment)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Integration>> GetActiveByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default)
    {
        var results = await _context.Integrations
            .AsNoTracking()
            .Where(i => i.Type == type && i.Status == IntegrationStatus.Active)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyCollection<Integration> Items, int TotalCount)> GetPagedAsync(
        IntegrationType? type = null,
        IntegrationStatus? status = null,
        IntegrationEnvironment? environment = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Integrations.AsNoTracking().AsQueryable();

        // Apply filters
        if (type.HasValue)
        {
            query = query.Where(i => i.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (environment.HasValue)
        {
            query = query.Where(i => i.Environment == environment.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchPattern = $"%{searchTerm.Trim()}%";
            query = query.Where(i =>
                EF.Functions.Like(i.Name, searchPattern) ||
                EF.Functions.Like(i.Description ?? string.Empty, searchPattern));
        }

        // Order by name
        query = query.OrderBy(i => i.Name).ThenBy(i => i.Id);

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

    public async Task AddAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        await _context.Integrations.AddAsync(integration, cancellationToken);
    }

    public void Update(Integration integration)
    {
        _context.Integrations.Update(integration);
    }

    public void Delete(Integration integration)
    {
        _context.Integrations.Remove(integration);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
