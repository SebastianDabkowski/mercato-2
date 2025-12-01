using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for review moderation audit logs.
/// </summary>
public sealed class ReviewModerationAuditLogRepository : IReviewModerationAuditLogRepository
{
    private readonly AppDbContext _context;

    public ReviewModerationAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ReviewModerationAuditLog>> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.ReviewModerationAuditLogs
            .AsNoTracking()
            .Where(l => l.ReviewId == reviewId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyList<ReviewModerationAuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? reviewId,
        Guid? moderatorId,
        ReviewModerationAction? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewModerationAuditLogs.AsNoTracking();

        if (reviewId.HasValue)
        {
            query = query.Where(l => l.ReviewId == reviewId.Value);
        }

        if (moderatorId.HasValue)
        {
            query = query.Where(l => l.ModeratorId == moderatorId.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(l => l.Action == action.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task AddAsync(ReviewModerationAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.ReviewModerationAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
