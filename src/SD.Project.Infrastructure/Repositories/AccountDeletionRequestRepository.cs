using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed account deletion request repository.
/// </summary>
public sealed class AccountDeletionRequestRepository : IAccountDeletionRequestRepository
{
    private readonly AppDbContext _context;

    public AccountDeletionRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AccountDeletionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccountDeletionRequests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<AccountDeletionRequest?> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountDeletionRequests
            .Where(r => r.UserId == userId && r.Status == AccountDeletionRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountDeletionRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountDeletionRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveDeletionRequestAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountDeletionRequests
            .AnyAsync(r => r.UserId == userId &&
                (r.Status == AccountDeletionRequestStatus.Pending ||
                 r.Status == AccountDeletionRequestStatus.Processing),
                cancellationToken);
    }

    public async Task AddAsync(AccountDeletionRequest request, CancellationToken cancellationToken = default)
    {
        await _context.AccountDeletionRequests.AddAsync(request, cancellationToken);
    }

    public void Update(AccountDeletionRequest request)
    {
        _context.AccountDeletionRequests.Update(request);
    }

    public async Task AddAuditLogAsync(AccountDeletionAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AccountDeletionAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountDeletionAuditLog>> GetAuditLogsForRequestAsync(Guid deletionRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountDeletionAuditLogs
            .AsNoTracking()
            .Where(l => l.DeletionRequestId == deletionRequestId)
            .OrderByDescending(l => l.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountDeletionAuditLog>> GetAuditLogsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountDeletionAuditLogs
            .AsNoTracking()
            .Where(l => l.AffectedUserId == userId || l.TriggeredByUserId == userId)
            .OrderByDescending(l => l.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
