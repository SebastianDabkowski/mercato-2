using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the user data export repository.
/// </summary>
public class UserDataExportRepository : IUserDataExportRepository
{
    private readonly AppDbContext _context;

    public UserDataExportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserDataExport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserDataExports
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserDataExport>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserDataExports
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDataExport?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserDataExports
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDataExport>> GetPendingExportsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserDataExports
            .AsNoTracking()
            .Where(e => e.Status == UserDataExportStatus.Pending)
            .OrderBy(e => e.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDataExport>> GetExpiredExportsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserDataExports
            .AsNoTracking()
            .Where(e => e.Status == UserDataExportStatus.Completed && 
                        e.ExpiresAt.HasValue && 
                        e.ExpiresAt.Value < now)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasRecentPendingExportAsync(Guid userId, int withinHours = 24, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-withinHours);
        return await _context.UserDataExports
            .AnyAsync(e => e.UserId == userId && 
                          (e.Status == UserDataExportStatus.Pending || e.Status == UserDataExportStatus.Processing) &&
                          e.RequestedAt > cutoff, 
                      cancellationToken);
    }

    public async Task AddAsync(UserDataExport export, CancellationToken cancellationToken = default)
    {
        await _context.UserDataExports.AddAsync(export, cancellationToken);
    }

    public void Update(UserDataExport export)
    {
        _context.UserDataExports.Update(export);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
