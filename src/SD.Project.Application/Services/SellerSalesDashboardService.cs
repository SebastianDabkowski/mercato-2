using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for seller sales dashboard.
/// </summary>
public sealed class SellerSalesDashboardService
{
    private readonly ISellerDashboardRepository _repository;

    public SellerSalesDashboardService(ISellerDashboardRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    /// Gets seller sales dashboard data for the specified period.
    /// </summary>
    public async Task<SellerSalesDashboardDto> HandleAsync(
        GetSellerSalesDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Ensure dates are in UTC and ToDate includes the entire day
        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddSeconds(-1);

        // Parse granularity
        var granularity = ParseGranularity(query.Granularity);

        // Get aggregated metrics (optionally filtered)
        SellerSalesMetrics metrics;
        if (query.ProductId.HasValue || !string.IsNullOrWhiteSpace(query.Category))
        {
            metrics = await _repository.GetFilteredSalesMetricsAsync(
                query.StoreId,
                query.ProductId,
                query.Category,
                fromDate,
                toDate,
                cancellationToken);
        }
        else
        {
            metrics = await _repository.GetSalesMetricsAsync(
                query.StoreId,
                fromDate,
                toDate,
                cancellationToken);
        }

        // Get time-series data
        var timeSeries = await _repository.GetSalesTimeSeriesAsync(
            query.StoreId,
            fromDate,
            toDate,
            granularity,
            cancellationToken);

        // Check if there is any data
        var hasData = metrics.Gmv > 0 || metrics.OrderCount > 0;

        // Map time-series data to DTOs
        var timeSeriesDto = timeSeries
            .Select(dp => new SellerSalesDataPointDto(
                dp.PeriodStart,
                dp.PeriodLabel,
                dp.Gmv,
                dp.OrderCount))
            .ToList();

        return new SellerSalesDashboardDto(
            metrics.Gmv,
            metrics.OrderCount,
            metrics.ItemCount,
            metrics.AverageOrderValue,
            Currency: "USD", // Default currency for the platform
            query.FromDate.Date,
            query.ToDate.Date,
            hasData,
            RefreshedAt: DateTime.UtcNow,
            timeSeriesDto,
            query.Granularity.ToLowerInvariant());
    }

    /// <summary>
    /// Gets filter options for the seller sales dashboard.
    /// </summary>
    public async Task<SellerDashboardFilterOptionsDto> HandleAsync(
        GetSellerDashboardFilterOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _repository.GetProductsWithSalesAsync(query.StoreId, cancellationToken);
        var categories = await _repository.GetCategoriesWithSalesAsync(query.StoreId, cancellationToken);

        var productDtos = products
            .Select(p => new ProductFilterOptionDto(p.ProductId, p.ProductName))
            .ToList();

        return new SellerDashboardFilterOptionsDto(productDtos, categories);
    }

    private static TimeGranularity ParseGranularity(string granularity)
    {
        return granularity.ToLowerInvariant() switch
        {
            "day" => TimeGranularity.Day,
            "week" => TimeGranularity.Week,
            "month" => TimeGranularity.Month,
            _ => TimeGranularity.Day
        };
    }
}
