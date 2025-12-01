using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for user data export persistence operations.
/// </summary>
public interface IUserDataExportRepository
{
    /// <summary>
    /// Gets a data export request by ID.
    /// </summary>
    Task<UserDataExport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all data export requests for a user.
    /// </summary>
    Task<IReadOnlyList<UserDataExport>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent data export request for a user.
    /// </summary>
    Task<UserDataExport?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending export requests that need to be processed.
    /// </summary>
    Task<IReadOnlyList<UserDataExport>> GetPendingExportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired exports that need cleanup.
    /// </summary>
    Task<IReadOnlyList<UserDataExport>> GetExpiredExportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user has a recent pending or processing export request.
    /// Used to prevent duplicate requests.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="withinHours">The time window to check (default: 24 hours).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasRecentPendingExportAsync(Guid userId, int withinHours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new data export request.
    /// </summary>
    Task AddAsync(UserDataExport export, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing data export request.
    /// </summary>
    void Update(UserDataExport export);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
