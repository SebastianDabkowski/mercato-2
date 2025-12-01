using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDataProcessingActivityRepository"/>.
/// </summary>
public sealed class DataProcessingActivityRepository : IDataProcessingActivityRepository
{
    private readonly AppDbContext _context;

    public DataProcessingActivityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DataProcessingActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DataProcessingActivity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivities
            .AsNoTracking()
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DataProcessingActivity>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivities
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DataProcessingActivity activity, CancellationToken cancellationToken = default)
    {
        await _context.DataProcessingActivities.AddAsync(activity, cancellationToken);
    }

    public void Update(DataProcessingActivity activity)
    {
        _context.DataProcessingActivities.Update(activity);
    }

    public async Task AddAuditLogAsync(DataProcessingActivityAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.DataProcessingActivityAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DataProcessingActivityAuditLog>> GetAuditLogsAsync(
        Guid dataProcessingActivityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingActivityAuditLogs
            .AsNoTracking()
            .Where(a => a.DataProcessingActivityId == dataProcessingActivityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
