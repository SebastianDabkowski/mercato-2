namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for marketplace dashboard metrics.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Gets total GMV (Gross Merchandise Value) for the given period.
    /// GMV is defined as gross order value including shipping.
    /// </summary>
    Task<decimal> GetGmvAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total number of orders for the given period.
    /// </summary>
    Task<int> GetOrderCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets number of active sellers for the given period.
    /// Active sellers are defined as sellers with at least one active product OR at least one order in the period.
    /// </summary>
    Task<int> GetActiveSellerCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets number of active products.
    /// Active products are products with status 'Active'.
    /// </summary>
    Task<int> GetActiveProductCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets number of newly registered users for the given period.
    /// </summary>
    Task<int> GetNewUserCountAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
