using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the user session repository.
/// </summary>
public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly AppDbContext _context;

    public UserSessionRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserSession?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        return await _context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await _context.UserSessions.AddAsync(session, cancellationToken);
    }

    public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        _context.UserSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CleanupExpiredSessionsAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);
        
        // Find sessions that are either expired or revoked and older than the cutoff
        var sessionsToRemove = await _context.UserSessions
            .Where(s => (s.ExpiresAt < cutoff) || (s.RevokedAt != null && s.RevokedAt < cutoff))
            .ToListAsync(cancellationToken);

        if (sessionsToRemove.Count == 0)
        {
            return 0;
        }

        _context.UserSessions.RemoveRange(sessionsToRemove);
        await _context.SaveChangesAsync(cancellationToken);
        
        return sessionsToRemove.Count;
    }
}
