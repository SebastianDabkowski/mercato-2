using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of seller-specific sales dashboard repository.
/// </summary>
public sealed class SellerDashboardRepository : ISellerDashboardRepository
{
    private readonly AppDbContext _context;

    public SellerDashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SellerSalesMetrics> GetSalesMetricsAsync(
        Guid storeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        // Get order items for this store within the date range
        var orderItems = await GetStoreOrderItemsQuery(storeId, fromDate, toDate)
            .ToListAsync(cancellationToken);

        return CalculateMetrics(orderItems);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerSalesDataPoint>> GetSalesTimeSeriesAsync(
        Guid storeId,
        DateTime fromDate,
        DateTime toDate,
        TimeGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        // Get order items for this store within the date range
        var orderItemsWithDates = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.StoreId == storeId)
            .Join(
                _context.Orders.Where(o =>
                    o.CreatedAt >= fromDate &&
                    o.CreatedAt <= toDate &&
                    o.Status != OrderStatus.Cancelled &&
                    o.Status != OrderStatus.PaymentFailed),
                oi => oi.OrderId,
                o => o.Id,
                (oi, o) => new { OrderItem = oi, OrderDate = o.CreatedAt, OrderId = o.Id })
            .ToListAsync(cancellationToken);

        // Group by period based on granularity
        var groupedData = orderItemsWithDates
            .GroupBy(x => GetPeriodStart(x.OrderDate, granularity))
            .Select(g => new SellerSalesDataPoint(
                g.Key,
                GetPeriodLabel(g.Key, granularity),
                g.Sum(x => x.OrderItem.LineTotal + x.OrderItem.ShippingCost),
                g.Select(x => x.OrderId).Distinct().Count()))
            .OrderBy(dp => dp.PeriodStart)
            .ToList();

        // Fill in missing periods with zero values
        return FillMissingPeriods(groupedData, fromDate, toDate, granularity);
    }

    /// <inheritdoc />
    public async Task<SellerSalesMetrics> GetFilteredSalesMetricsAsync(
        Guid storeId,
        Guid? productId,
        string? category,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var query = GetStoreOrderItemsQuery(storeId, fromDate, toDate);

        // Filter by product if specified
        if (productId.HasValue)
        {
            query = query.Where(oi => oi.ProductId == productId.Value);
        }

        // Filter by category if specified
        if (!string.IsNullOrWhiteSpace(category))
        {
            // Join with products to get category
            var productIds = await _context.Products
                .AsNoTracking()
                .Where(p => p.StoreId == storeId && p.Category == category)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(oi => productIds.Contains(oi.ProductId));
        }

        var orderItems = await query.ToListAsync(cancellationToken);
        return CalculateMetrics(orderItems);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerProductFilterOption>> GetProductsWithSalesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        // Get unique products that have been sold by this store
        var productIds = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.StoreId == storeId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get product names
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new SellerProductFilterOption(p.Id, p.Name))
            .OrderBy(p => p.ProductName)
            .ToListAsync(cancellationToken);

        return products;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCategoriesWithSalesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        // Get unique products that have been sold by this store
        var productIds = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.StoreId == storeId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get unique categories from those products
        var categories = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !string.IsNullOrEmpty(p.Category))
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        return categories;
    }

    private IQueryable<OrderItem> GetStoreOrderItemsQuery(Guid storeId, DateTime fromDate, DateTime toDate)
    {
        return _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.StoreId == storeId)
            .Join(
                _context.Orders.Where(o =>
                    o.CreatedAt >= fromDate &&
                    o.CreatedAt <= toDate &&
                    o.Status != OrderStatus.Cancelled &&
                    o.Status != OrderStatus.PaymentFailed),
                oi => oi.OrderId,
                o => o.Id,
                (oi, o) => oi);
    }

    private static SellerSalesMetrics CalculateMetrics(IReadOnlyList<OrderItem> orderItems)
    {
        if (orderItems.Count == 0)
        {
            return new SellerSalesMetrics(0m, 0, 0, 0m);
        }

        var gmv = orderItems.Sum(oi => oi.LineTotal + oi.ShippingCost);
        var orderCount = orderItems.Select(oi => oi.OrderId).Distinct().Count();
        var itemCount = orderItems.Sum(oi => oi.Quantity);
        var averageOrderValue = orderCount > 0 ? gmv / orderCount : 0m;

        return new SellerSalesMetrics(gmv, orderCount, itemCount, averageOrderValue);
    }

    private static DateTime GetPeriodStart(DateTime date, TimeGranularity granularity)
    {
        return granularity switch
        {
            TimeGranularity.Day => date.Date,
            TimeGranularity.Week => date.Date.AddDays(-(int)date.DayOfWeek),
            TimeGranularity.Month => new DateTime(date.Year, date.Month, 1),
            _ => date.Date
        };
    }

    private static string GetPeriodLabel(DateTime periodStart, TimeGranularity granularity)
    {
        return granularity switch
        {
            TimeGranularity.Day => periodStart.ToString("MMM dd"),
            TimeGranularity.Week => $"Week of {periodStart:MMM dd}",
            TimeGranularity.Month => periodStart.ToString("MMM yyyy"),
            _ => periodStart.ToString("MMM dd")
        };
    }

    private static IReadOnlyList<SellerSalesDataPoint> FillMissingPeriods(
        List<SellerSalesDataPoint> existingData,
        DateTime fromDate,
        DateTime toDate,
        TimeGranularity granularity)
    {
        var result = new List<SellerSalesDataPoint>();
        var existingByPeriod = existingData.ToDictionary(dp => dp.PeriodStart);

        var currentPeriod = GetPeriodStart(fromDate, granularity);
        var endPeriod = GetPeriodStart(toDate, granularity);

        while (currentPeriod <= endPeriod)
        {
            if (existingByPeriod.TryGetValue(currentPeriod, out var existingPoint))
            {
                result.Add(existingPoint);
            }
            else
            {
                result.Add(new SellerSalesDataPoint(
                    currentPeriod,
                    GetPeriodLabel(currentPeriod, granularity),
                    0m,
                    0));
            }

            currentPeriod = granularity switch
            {
                TimeGranularity.Day => currentPeriod.AddDays(1),
                TimeGranularity.Week => currentPeriod.AddDays(7),
                TimeGranularity.Month => currentPeriod.AddMonths(1),
                _ => currentPeriod.AddDays(1)
            };
        }

        return result;
    }
}
