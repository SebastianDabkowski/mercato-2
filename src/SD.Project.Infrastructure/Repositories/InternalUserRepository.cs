using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the internal user repository.
/// </summary>
public sealed class InternalUserRepository : IInternalUserRepository
{
    private readonly AppDbContext _context;

    public InternalUserRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InternalUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUsers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<InternalUser?> GetByStoreAndUserIdAsync(Guid storeId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUsers
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.UserId == userId, cancellationToken);
    }

    public async Task<InternalUser?> GetByStoreAndEmailAsync(Guid storeId, Email email, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUsers
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<InternalUser>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUsers
            .AsNoTracking()
            .Where(x => x.StoreId == storeId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InternalUser>> GetActiveByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUsers
            .AsNoTracking()
            .Where(x => x.StoreId == storeId && x.Status == InternalUserStatus.Active)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByStoreAndEmailAsync(Guid storeId, Email email, CancellationToken cancellationToken = default)
    {
        return await _context.InternalUsers
            .AnyAsync(x => x.StoreId == storeId && x.Email == email, cancellationToken);
    }

    public async Task AddAsync(InternalUser internalUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(internalUser);
        await _context.InternalUsers.AddAsync(internalUser, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
