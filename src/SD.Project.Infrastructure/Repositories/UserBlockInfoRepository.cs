using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of user block info repository.
/// </summary>
public sealed class UserBlockInfoRepository : IUserBlockInfoRepository
{
    private readonly AppDbContext _context;

    public UserBlockInfoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserBlockInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserBlockInfos
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<UserBlockInfo?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserBlockInfos
            .FirstOrDefaultAsync(b => b.UserId == userId && b.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<UserBlockInfo>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var results = await _context.UserBlockInfos
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BlockedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task AddAsync(UserBlockInfo blockInfo, CancellationToken cancellationToken = default)
    {
        await _context.UserBlockInfos.AddAsync(blockInfo, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
