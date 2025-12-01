using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for seller rating moderation audit logs.
/// </summary>
public sealed class SellerRatingModerationAuditLogRepository : ISellerRatingModerationAuditLogRepository
{
    private readonly AppDbContext _context;

    public SellerRatingModerationAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SellerRatingModerationAuditLog>> GetBySellerRatingIdAsync(
        Guid sellerRatingId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.SellerRatingModerationAuditLogs
            .AsNoTracking()
            .Where(l => l.SellerRatingId == sellerRatingId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyList<SellerRatingModerationAuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? sellerRatingId,
        Guid? moderatorId,
        SellerRatingModerationAction? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SellerRatingModerationAuditLogs.AsNoTracking();

        if (sellerRatingId.HasValue)
        {
            query = query.Where(l => l.SellerRatingId == sellerRatingId.Value);
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

    public async Task AddAsync(SellerRatingModerationAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.SellerRatingModerationAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
