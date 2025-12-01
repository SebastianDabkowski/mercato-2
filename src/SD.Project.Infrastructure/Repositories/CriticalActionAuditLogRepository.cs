using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the critical action audit log repository.
/// </summary>
public sealed class CriticalActionAuditLogRepository : ICriticalActionAuditLogRepository
{
    private readonly AppDbContext _context;

    public CriticalActionAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(CriticalActionAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.CriticalActionAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CriticalActionAuditLog>> GetByUserIdAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.CriticalActionAuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CriticalActionAuditLog>> GetByActionTypeAsync(
        CriticalActionType actionType,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.CriticalActionAuditLogs
            .AsNoTracking()
            .Where(a => a.ActionType == actionType)
            .OrderByDescending(a => a.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CriticalActionAuditLog>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CriticalActionType? actionType = null,
        CriticalActionOutcome? outcome = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CriticalActionAuditLogs
            .AsNoTracking()
            .Where(a => a.OccurredAt >= fromDate && a.OccurredAt <= toDate);

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (actionType.HasValue)
        {
            query = query.Where(a => a.ActionType == actionType.Value);
        }

        if (outcome.HasValue)
        {
            query = query.Where(a => a.Outcome == outcome.Value);
        }

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CriticalActionAuditLog>> GetByTargetResourceAsync(
        string targetResourceType,
        Guid targetResourceId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.CriticalActionAuditLogs
            .AsNoTracking()
            .Where(a => a.TargetResourceType == targetResourceType && a.TargetResourceId == targetResourceId)
            .OrderByDescending(a => a.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CriticalActionType? actionType = null,
        CriticalActionOutcome? outcome = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CriticalActionAuditLogs
            .Where(a => a.OccurredAt >= fromDate && a.OccurredAt <= toDate);

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (actionType.HasValue)
        {
            query = query.Where(a => a.ActionType == actionType.Value);
        }

        if (outcome.HasValue)
        {
            query = query.Where(a => a.Outcome == outcome.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // Use ExecuteDeleteAsync for better performance - avoids loading records into memory
        var deletedCount = await _context.CriticalActionAuditLogs
            .Where(a => a.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
