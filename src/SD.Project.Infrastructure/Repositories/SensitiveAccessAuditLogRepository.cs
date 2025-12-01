using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the sensitive access audit log repository.
/// </summary>
public sealed class SensitiveAccessAuditLogRepository : ISensitiveAccessAuditLogRepository
{
    private readonly AppDbContext _context;

    public SensitiveAccessAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(SensitiveAccessAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.SensitiveAccessAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByResourceAsync(
        SensitiveResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SensitiveAccessAuditLogs
            .AsNoTracking()
            .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
            .OrderByDescending(a => a.AccessedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByAccessorAsync(
        Guid accessedByUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.SensitiveAccessAuditLogs
            .AsNoTracking()
            .Where(a => a.AccessedByUserId == accessedByUserId)
            .OrderByDescending(a => a.AccessedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByResourceOwnerAsync(
        Guid resourceOwnerId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.SensitiveAccessAuditLogs
            .AsNoTracking()
            .Where(a => a.ResourceOwnerId == resourceOwnerId)
            .OrderByDescending(a => a.AccessedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        SensitiveResourceType? resourceType = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SensitiveAccessAuditLogs
            .AsNoTracking()
            .Where(a => a.AccessedAt >= fromDate && a.AccessedAt <= toDate);

        if (resourceType.HasValue)
        {
            query = query.Where(a => a.ResourceType == resourceType.Value);
        }

        return await query
            .OrderByDescending(a => a.AccessedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
