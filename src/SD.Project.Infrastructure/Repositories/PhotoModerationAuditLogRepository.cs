using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the photo moderation audit log repository.
/// </summary>
public sealed class PhotoModerationAuditLogRepository : IPhotoModerationAuditLogRepository
{
    private readonly AppDbContext _context;

    public PhotoModerationAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PhotoModerationAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PhotoModerationAuditLogs
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PhotoModerationAuditLog>> GetByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        return await _context.PhotoModerationAuditLogs
            .Where(l => l.PhotoId == photoId)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PhotoModerationAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.PhotoModerationAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
