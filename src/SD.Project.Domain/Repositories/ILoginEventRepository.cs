using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for managing login event records.
/// </summary>
public interface ILoginEventRepository
{
    /// <summary>
    /// Adds a new login event.
    /// </summary>
    /// <param name="loginEvent">The login event to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(LoginEvent loginEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent login events for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="count">Maximum number of events to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent login events, ordered by most recent first.</returns>
    Task<IReadOnlyList<LoginEvent>> GetRecentByUserIdAsync(Guid userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login events for a user within a specified time range.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="since">Start of the time range (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of login events within the time range.</returns>
    Task<IReadOnlyList<LoginEvent>> GetByUserIdSinceAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts failed login attempts for a user within a specified time range.
    /// Used for detecting unusual login activity.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="since">Start of the time range (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of failed login attempts.</returns>
    Task<int> CountFailedLoginsSinceAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct IP addresses used for login by a user within a specified time range.
    /// Used for detecting logins from new locations.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="since">Start of the time range (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of distinct IP addresses.</returns>
    Task<IReadOnlyList<string>> GetDistinctIpAddressesAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes login events that have exceeded their retention period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of records removed.</returns>
    Task<int> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
