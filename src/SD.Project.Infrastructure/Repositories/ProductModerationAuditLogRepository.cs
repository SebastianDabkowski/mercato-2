using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for ProductModerationAuditLog entities.
/// </summary>
public sealed class ProductModerationAuditLogRepository : IProductModerationAuditLogRepository
{
    private readonly AppDbContext _context;

    public ProductModerationAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ProductModerationAuditLog>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.ProductModerationAuditLogs
            .AsNoTracking()
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyCollection<ProductModerationAuditLog> Items, int TotalCount)> GetByModeratorIdAsync(
        Guid moderatorId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductModerationAuditLogs
            .AsNoTracking()
            .Where(a => a.ModeratorId == moderatorId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var skip = (pageNumber - 1) * pageSize;
        var results = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (results.AsReadOnly(), totalCount);
    }

    public async Task AddAsync(ProductModerationAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.ProductModerationAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
