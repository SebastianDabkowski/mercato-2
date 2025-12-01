using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for feature flags.
/// </summary>
public sealed class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly AppDbContext _context;

    public FeatureFlagRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var keyLower = key.ToLowerInvariant();
        return await _context.FeatureFlags.FirstOrDefaultAsync(x => x.Key == keyLower, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Key)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyCollection<FeatureFlag> Items, int TotalCount)> GetAllPagedAsync(
        string? searchTerm = null,
        FeatureFlagStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FeatureFlags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchPattern = $"%{searchTerm.Trim()}%";
            query = query.Where(f =>
                EF.Functions.Like(f.Key, searchPattern) ||
                EF.Functions.Like(f.Name, searchPattern) ||
                EF.Functions.Like(f.Description, searchPattern));
        }

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var skip = (pageNumber - 1) * pageSize;
        var results = await query
            .OrderBy(f => f.Key)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (results.AsReadOnly(), totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FeatureFlagEnvironment>> GetEnvironmentsByFlagIdAsync(
        Guid featureFlagId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.FeatureFlagEnvironments
            .AsNoTracking()
            .Where(e => e.FeatureFlagId == featureFlagId)
            .OrderBy(e => e.Environment)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<FeatureFlagEnvironment?> GetEnvironmentAsync(
        Guid featureFlagId,
        string environment,
        CancellationToken cancellationToken = default)
    {
        var envLower = environment.ToLowerInvariant();
        return await _context.FeatureFlagEnvironments
            .FirstOrDefaultAsync(
                e => e.FeatureFlagId == featureFlagId && e.Environment == envLower,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FeatureFlagAuditLog>> GetAuditLogsByFlagIdAsync(
        Guid featureFlagId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var skip = (pageNumber - 1) * pageSize;
        var results = await _context.FeatureFlagAuditLogs
            .AsNoTracking()
            .Where(l => l.FeatureFlagId == featureFlagId)
            .OrderByDescending(l => l.OccurredAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyCollection<FeatureFlagAuditLog> Items, int TotalCount)> GetAuditLogsAsync(
        Guid? featureFlagId = null,
        Guid? userId = null,
        FeatureFlagAuditAction? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FeatureFlagAuditLogs.AsNoTracking().AsQueryable();

        if (featureFlagId.HasValue)
        {
            query = query.Where(l => l.FeatureFlagId == featureFlagId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.PerformedByUserId == userId.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(l => l.Action == action.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.OccurredAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.OccurredAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var skip = (pageNumber - 1) * pageSize;
        var results = await query
            .OrderByDescending(l => l.OccurredAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (results.AsReadOnly(), totalCount);
    }

    /// <inheritdoc />
    public async Task AddAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default)
    {
        await _context.FeatureFlags.AddAsync(featureFlag, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddEnvironmentAsync(FeatureFlagEnvironment environment, CancellationToken cancellationToken = default)
    {
        await _context.FeatureFlagEnvironments.AddAsync(environment, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAuditLogAsync(FeatureFlagAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.FeatureFlagAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(FeatureFlag featureFlag)
    {
        _context.FeatureFlags.Update(featureFlag);
    }

    /// <inheritdoc />
    public void UpdateEnvironment(FeatureFlagEnvironment environment)
    {
        _context.FeatureFlagEnvironments.Update(environment);
    }

    /// <inheritdoc />
    public void Delete(FeatureFlag featureFlag)
    {
        // Delete related environment configurations
        // Note: Using RemoveRange for compatibility with InMemory provider (ExecuteDeleteAsync not supported).
        // For relational databases, consider using ExecuteDeleteAsync for better performance.
        var environments = _context.FeatureFlagEnvironments
            .Where(e => e.FeatureFlagId == featureFlag.Id);
        _context.FeatureFlagEnvironments.RemoveRange(environments);

        // Delete the feature flag
        _context.FeatureFlags.Remove(featureFlag);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
