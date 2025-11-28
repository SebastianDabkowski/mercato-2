using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the internal user invitation repository.
/// </summary>
public sealed class InternalUserInvitationRepository : IInternalUserInvitationRepository
{
    private readonly AppDbContext _context;

    public InternalUserInvitationRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InternalUserInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUserInvitations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<InternalUserInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        return await _context.InternalUserInvitations
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<InternalUserInvitation?> GetLatestByInternalUserIdAsync(Guid internalUserId, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUserInvitations
            .Where(x => x.InternalUserId == internalUserId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(InternalUserInvitation invitation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        await _context.InternalUserInvitations.AddAsync(invitation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
