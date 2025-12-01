using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of user analytics repository.
/// Returns only aggregated, anonymised data for privacy compliance.
/// </summary>
public sealed class UserAnalyticsRepository : IUserAnalyticsRepository
{
    private readonly AppDbContext _context;

    public UserAnalyticsRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the count of new buyer accounts registered in the specified period.
    /// </summary>
    public async Task<int> GetNewBuyerCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Buyer)
            .Where(u => u.CreatedAt >= fromDate && u.CreatedAt <= toDate)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the count of new seller accounts registered in the specified period.
    /// </summary>
    public async Task<int> GetNewSellerCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Seller)
            .Where(u => u.CreatedAt >= fromDate && u.CreatedAt <= toDate)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the count of users who logged in at least once in the specified period.
    /// This is the definition of 'active user' for analytics purposes.
    /// </summary>
    public async Task<int> GetActiveUserCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // Count distinct users who have a successful login event in the period
        return await _context.LoginEvents
            .AsNoTracking()
            .Where(e => e.IsSuccess)
            .Where(e => e.UserId.HasValue)
            .Where(e => e.OccurredAt >= fromDate && e.OccurredAt <= toDate)
            .Select(e => e.UserId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the count of users who placed at least one order in the specified period.
    /// </summary>
    public async Task<int> GetUsersWithOrdersCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // Count distinct buyers who have placed orders in the period
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .Select(o => o.BuyerId)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
