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
            .Where(r => r.StoreId == storeId && r.ModerationStatus == SellerRatingModerationStatus.Approved)
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
            .Where(r => r.StoreId == storeId && r.ModerationStatus == SellerRatingModerationStatus.Approved)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (ratings.Count == 0)
        {
            return (0, 0);
        }

        return (ratings.Average(), ratings.Count);
    }

    public async Task<(IReadOnlyList<SellerRating> Items, int TotalCount)> GetForModerationPagedAsync(
        SellerRatingModerationStatus? status,
        bool? isFlagged,
        string? searchTerm,
        Guid? storeId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SellerRatings.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.ModerationStatus == status.Value);
        }

        if (isFlagged.HasValue)
        {
            query = query.Where(r => r.IsFlagged == isFlagged.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(r => r.Comment != null && r.Comment.ToLower().Contains(term));
        }

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sort by priority: flagged first, then pending, then by date
        var items = await query
            .OrderByDescending(r => r.IsFlagged)
            .ThenBy(r => r.ModerationStatus == SellerRatingModerationStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task<SellerRatingModerationStats> GetModerationStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var pendingCount = await _context.SellerRatings
            .CountAsync(r => r.ModerationStatus == SellerRatingModerationStatus.Pending, cancellationToken);

        var flaggedCount = await _context.SellerRatings
            .CountAsync(r => r.IsFlagged, cancellationToken);

        var reportedCount = await _context.SellerRatings
            .CountAsync(r => r.ReportCount > 0, cancellationToken);

        var approvedTodayCount = await _context.SellerRatings
            .CountAsync(r => r.ModerationStatus == SellerRatingModerationStatus.Approved 
                && r.ModeratedAt >= today && r.ModeratedAt < tomorrow, cancellationToken);

        var rejectedTodayCount = await _context.SellerRatings
            .CountAsync(r => r.ModerationStatus == SellerRatingModerationStatus.Rejected 
                && r.ModeratedAt >= today && r.ModeratedAt < tomorrow, cancellationToken);

        return new SellerRatingModerationStats(
            pendingCount,
            flaggedCount,
            reportedCount,
            approvedTodayCount,
            rejectedTodayCount);
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
