using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for seller reputations.
/// </summary>
public sealed class SellerReputationRepository : ISellerReputationRepository
{
    private readonly AppDbContext _context;

    public SellerReputationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SellerReputation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SellerReputations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<SellerReputation?> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerReputations
            .FirstOrDefaultAsync(r => r.StoreId == storeId, cancellationToken);
    }

    public async Task<IReadOnlyList<SellerReputation>> GetByStoreIdsAsync(
        IEnumerable<Guid> storeIds,
        CancellationToken cancellationToken = default)
    {
        var storeIdList = storeIds.ToList();
        var results = await _context.SellerReputations
            .AsNoTracking()
            .Where(r => storeIdList.Contains(r.StoreId))
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<SellerReputation>> GetTopSellersAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.SellerReputations
            .AsNoTracking()
            .OrderByDescending(r => r.ReputationScore)
            .Take(take)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<SellerReputation>> GetStaleReputationsAsync(
        DateTime olderThan,
        int take,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.SellerReputations
            .Where(r => r.LastCalculatedAt < olderThan)
            .OrderBy(r => r.LastCalculatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task AddAsync(SellerReputation reputation, CancellationToken cancellationToken = default)
    {
        await _context.SellerReputations.AddAsync(reputation, cancellationToken);
    }

    public void Update(SellerReputation reputation)
    {
        _context.SellerReputations.Update(reputation);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
