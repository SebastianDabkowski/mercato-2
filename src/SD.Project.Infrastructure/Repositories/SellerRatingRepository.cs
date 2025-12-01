using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for seller ratings.
/// </summary>
public sealed class SellerRatingRepository : ISellerRatingRepository
{
    private readonly AppDbContext _context;

    public SellerRatingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SellerRating?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SellerRatings.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<SellerRating?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerRatings
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<SellerRating>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.SellerRatings
            .AsNoTracking()
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(double AverageRating, int RatingCount)> GetStoreRatingStatsAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var ratings = await _context.SellerRatings
            .AsNoTracking()
            .Where(r => r.StoreId == storeId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (ratings.Count == 0)
        {
            return (0, 0);
        }

        return (ratings.Average(), ratings.Count);
    }

    public async Task AddAsync(SellerRating rating, CancellationToken cancellationToken = default)
    {
        await _context.SellerRatings.AddAsync(rating, cancellationToken);
    }

    public void Update(SellerRating rating)
    {
        _context.SellerRatings.Update(rating);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
