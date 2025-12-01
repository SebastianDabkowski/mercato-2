using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for user analytics metrics - provides aggregated, anonymised data only.
/// </summary>
public interface IUserAnalyticsRepository
{
    /// <summary>
    /// Gets the count of new buyer accounts registered in the specified period.
    /// </summary>
    Task<int> GetNewBuyerCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of new seller accounts registered in the specified period.
    /// </summary>
    Task<int> GetNewSellerCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users who logged in at least once in the specified period (active users).
    /// </summary>
    Task<int> GetActiveUserCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users who placed at least one order in the specified period.
    /// </summary>
    Task<int> GetUsersWithOrdersCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
