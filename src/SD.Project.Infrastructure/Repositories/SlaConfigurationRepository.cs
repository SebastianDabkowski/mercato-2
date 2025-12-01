using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of SLA configuration persistence.
/// </summary>
public sealed class SlaConfigurationRepository : ISlaConfigurationRepository
{
    private readonly AppDbContext _context;

    public SlaConfigurationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SlaConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SlaConfigurations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<SlaConfiguration?> GetByCategoryAsync(SlaCaseCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.SlaConfigurations
            .FirstOrDefaultAsync(c => c.Category == category, cancellationToken);
    }

    public async Task<IReadOnlyList<SlaConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _context.SlaConfigurations
            .OrderBy(c => c.Category)
            .ToListAsync(cancellationToken);

        return configs.AsReadOnly();
    }

    public async Task<SlaConfiguration?> GetEffectiveConfigAsync(SlaCaseCategory category, CancellationToken cancellationToken = default)
    {
        // First try to get the specific category config
        var specificConfig = await _context.SlaConfigurations
            .FirstOrDefaultAsync(c => c.Category == category && c.IsEnabled, cancellationToken);

        if (specificConfig is not null)
        {
            return specificConfig;
        }

        // Fall back to the default config
        return await _context.SlaConfigurations
            .FirstOrDefaultAsync(c => c.Category == SlaCaseCategory.Default && c.IsEnabled, cancellationToken);
    }

    public async Task<bool> ExistsForCategoryAsync(SlaCaseCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.SlaConfigurations
            .AnyAsync(c => c.Category == category, cancellationToken);
    }

    public async Task AddAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _context.SlaConfigurations.AddAsync(configuration, cancellationToken);
    }

    public Task UpdateAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _context.SlaConfigurations.Update(configuration);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _context.SlaConfigurations.Remove(configuration);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
