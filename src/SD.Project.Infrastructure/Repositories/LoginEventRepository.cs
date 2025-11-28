using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the login event repository.
/// </summary>
public sealed class LoginEventRepository : ILoginEventRepository
{
    private readonly AppDbContext _context;

    public LoginEventRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(LoginEvent loginEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loginEvent);
        await _context.LoginEvents.AddAsync(loginEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<LoginEvent>> GetRecentByUserIdAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return Array.Empty<LoginEvent>();
        }

        return await _context.LoginEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoginEvent>> GetByUserIdSinceAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _context.LoginEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.OccurredAt >= since)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountFailedLoginsSinceAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _context.LoginEvents
            .AsNoTracking()
            .CountAsync(e => e.UserId == userId && !e.IsSuccess && e.OccurredAt >= since, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctIpAddressesAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _context.LoginEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.OccurredAt >= since && e.IpAddress != null)
            .Select(e => e.IpAddress!)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredRecords = await _context.LoginEvents
            .Where(e => e.RetentionExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredRecords.Count == 0)
        {
            return 0;
        }

        _context.LoginEvents.RemoveRange(expiredRecords);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredRecords.Count;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
