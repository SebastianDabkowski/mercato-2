using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed email verification token repository.
/// </summary>
public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _context;

    public EmailVerificationTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetLatestValidTokenForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.EmailVerificationTokens
            .Where(x => x.UserId == userId && x.UsedAt == null && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
