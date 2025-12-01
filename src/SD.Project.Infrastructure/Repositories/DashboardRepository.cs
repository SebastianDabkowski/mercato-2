using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of dashboard metrics repository.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets total GMV (Gross Merchandise Value) for the given period.
    /// GMV is defined as gross order value including shipping.
    /// </summary>
    public async Task<decimal> GetGmvAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => IsWithinDateRange(o.CreatedAt, fromDate, toDate))
            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.PaymentFailed)
            .SumAsync(o => o.TotalAmount, cancellationToken);
    }

    /// <summary>
    /// Gets total number of orders for the given period.
    /// </summary>
    public async Task<int> GetOrderCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => IsWithinDateRange(o.CreatedAt, fromDate, toDate))
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets number of active sellers for the given period.
    /// Active sellers are defined as sellers with at least one active product OR at least one order in the period.
    /// </summary>
    public async Task<int> GetActiveSellerCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // Get stores with at least one active product
        var storesWithActiveProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.StoreId.HasValue && p.Status == ProductStatus.Active)
            .Select(p => p.StoreId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get stores with at least one order in the period
        var storesWithOrders = await _context.OrderItems
            .AsNoTracking()
            .Join(_context.Orders,
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { item.StoreId, order.CreatedAt })
            .Where(x => IsWithinDateRange(x.CreatedAt, fromDate, toDate))
            .Select(x => x.StoreId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Combine both sets of active sellers
        var allActiveStores = storesWithActiveProducts.Union(storesWithOrders).Distinct();

        return allActiveStores.Count();
    }

    /// <summary>
    /// Gets number of active products.
    /// Active products are products with status 'Active'.
    /// </summary>
    public async Task<int> GetActiveProductCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets number of newly registered users for the given period.
    /// </summary>
    public async Task<int> GetNewUserCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => IsWithinDateRange(u.CreatedAt, fromDate, toDate))
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a timestamp is within the specified date range (inclusive).
    /// </summary>
    private static bool IsWithinDateRange(DateTime timestamp, DateTime fromDate, DateTime toDate)
    {
        return timestamp >= fromDate && timestamp <= toDate;
    }
}
