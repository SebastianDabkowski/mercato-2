using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for data processing activity persistence operations.
/// </summary>
public interface IDataProcessingActivityRepository
{
    /// <summary>
    /// Gets a data processing activity by its ID.
    /// </summary>
    Task<DataProcessingActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all data processing activities.
    /// </summary>
    Task<IReadOnlyCollection<DataProcessingActivity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active data processing activities.
    /// </summary>
    Task<IReadOnlyCollection<DataProcessingActivity>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new data processing activity.
    /// </summary>
    Task AddAsync(DataProcessingActivity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing data processing activity.
    /// </summary>
    void Update(DataProcessingActivity activity);

    /// <summary>
    /// Adds an audit log entry for a data processing activity.
    /// </summary>
    Task AddAuditLogAsync(DataProcessingActivityAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit log entries for a specific data processing activity.
    /// </summary>
    Task<IReadOnlyCollection<DataProcessingActivityAuditLog>> GetAuditLogsAsync(
        Guid dataProcessingActivityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
