using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for reviews.
/// </summary>
public sealed class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Review?> GetByOrderShipmentProductAsync(
        Guid orderId,
        Guid shipmentId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => 
                r.OrderId == orderId && 
                r.ShipmentId == shipmentId && 
                r.ProductId == productId, 
                cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductIdPagedAsync(
        Guid productId,
        ReviewSortOrder sortOrder,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.ModerationStatus == ReviewModerationStatus.Approved);

        // Apply sorting
        query = sortOrder switch
        {
            ReviewSortOrder.HighestRating => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            ReviewSortOrder.LowestRating => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task<IReadOnlyList<Review>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Review>> GetByBuyerIdAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<int> GetReviewCountByBuyerInWindowAsync(
        Guid buyerId,
        DateTime windowStart,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .CountAsync(r => r.BuyerId == buyerId && r.CreatedAt >= windowStart, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetPendingReviewsAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetForModerationPagedAsync(
        ReviewModerationStatus? status,
        bool? isFlagged,
        string? searchTerm,
        Guid? storeId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reviews.AsNoTracking();

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
            .ThenBy(r => r.ModerationStatus == ReviewModerationStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task<IReadOnlyList<Review>> GetFlaggedReviewsAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.IsFlagged)
            .OrderByDescending(r => r.FlaggedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Review>> GetReportedReviewsAsync(
        int minReportCount,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ReportCount >= minReportCount)
            .OrderByDescending(r => r.ReportCount)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task<ReviewModerationStats> GetModerationStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var pendingCount = await _context.Reviews
            .CountAsync(r => r.ModerationStatus == ReviewModerationStatus.Pending, cancellationToken);

        var flaggedCount = await _context.Reviews
            .CountAsync(r => r.IsFlagged, cancellationToken);

        var reportedCount = await _context.Reviews
            .CountAsync(r => r.ReportCount > 0, cancellationToken);

        var approvedTodayCount = await _context.Reviews
            .CountAsync(r => r.ModerationStatus == ReviewModerationStatus.Approved 
                && r.ModeratedAt >= today && r.ModeratedAt < tomorrow, cancellationToken);

        var rejectedTodayCount = await _context.Reviews
            .CountAsync(r => r.ModerationStatus == ReviewModerationStatus.Rejected 
                && r.ModeratedAt >= today && r.ModeratedAt < tomorrow, cancellationToken);

        return new ReviewModerationStats(
            pendingCount,
            flaggedCount,
            reportedCount,
            approvedTodayCount,
            rejectedTodayCount);
    }

    public async Task<(double AverageRating, int ReviewCount)> GetProductRatingAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return (0, 0);
        }

        return (reviews.Average(), reviews.Count);
    }

    public async Task<(double AverageRating, int ReviewCount)> GetStoreRatingAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return (0, 0);
        }

        return (reviews.Average(), reviews.Count);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _context.Reviews.AddAsync(review, cancellationToken);
    }

    public void Update(Review review)
    {
        _context.Reviews.Update(review);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
