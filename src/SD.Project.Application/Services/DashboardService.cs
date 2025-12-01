using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for admin dashboard metrics.
/// </summary>
public sealed class DashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        ArgumentNullException.ThrowIfNull(dashboardRepository);
        _dashboardRepository = dashboardRepository;
    }

    /// <summary>
    /// Gets dashboard metrics for the specified period.
    /// </summary>
    public async Task<DashboardMetricsDto> HandleAsync(
        GetDashboardMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Ensure dates are in UTC and ToDate includes the entire day
        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddTicks(-1);

        var gmv = await _dashboardRepository.GetGmvAsync(fromDate, toDate, cancellationToken);
        var orderCount = await _dashboardRepository.GetOrderCountAsync(fromDate, toDate, cancellationToken);
        var activeSellerCount = await _dashboardRepository.GetActiveSellerCountAsync(fromDate, toDate, cancellationToken);
        var activeProductCount = await _dashboardRepository.GetActiveProductCountAsync(cancellationToken);
        var newUserCount = await _dashboardRepository.GetNewUserCountAsync(fromDate, toDate, cancellationToken);

        // Check if there is any period-specific data (GMV, orders, new users)
        // Active products and sellers are not strictly time-bound so we don't use them for this check
        var hasData = gmv > 0 || orderCount > 0 || newUserCount > 0;

        return new DashboardMetricsDto(
            gmv,
            orderCount,
            activeSellerCount,
            activeProductCount,
            newUserCount,
            Currency: "USD", // Default currency for the platform
            query.FromDate.Date,
            query.ToDate.Date,
            hasData,
            RefreshedAt: DateTime.UtcNow);
    }
}
