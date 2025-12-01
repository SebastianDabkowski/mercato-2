using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for user registration and activity analytics.
/// Provides aggregated, anonymised metrics only.
/// </summary>
public sealed class UserAnalyticsService
{
    private readonly IUserAnalyticsRepository _userAnalyticsRepository;

    public UserAnalyticsService(IUserAnalyticsRepository userAnalyticsRepository)
    {
        ArgumentNullException.ThrowIfNull(userAnalyticsRepository);
        _userAnalyticsRepository = userAnalyticsRepository;
    }

    /// <summary>
    /// Gets user analytics metrics for the specified period.
    /// </summary>
    public async Task<UserAnalyticsDto> HandleAsync(
        GetUserAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Ensure dates are in UTC and ToDate includes the entire day
        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddSeconds(-1);

        var newBuyerCount = await _userAnalyticsRepository.GetNewBuyerCountAsync(fromDate, toDate, cancellationToken);
        var newSellerCount = await _userAnalyticsRepository.GetNewSellerCountAsync(fromDate, toDate, cancellationToken);
        var activeUserCount = await _userAnalyticsRepository.GetActiveUserCountAsync(fromDate, toDate, cancellationToken);
        var usersWithOrdersCount = await _userAnalyticsRepository.GetUsersWithOrdersCountAsync(fromDate, toDate, cancellationToken);

        // Check if there is any data for the period
        var hasData = newBuyerCount > 0 || newSellerCount > 0 || activeUserCount > 0 || usersWithOrdersCount > 0;

        return new UserAnalyticsDto(
            newBuyerCount,
            newSellerCount,
            activeUserCount,
            usersWithOrdersCount,
            query.FromDate.Date,
            query.ToDate.Date,
            hasData,
            RefreshedAt: DateTime.UtcNow);
    }
}
