using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for critical action audit logs.
/// Provides query methods for security officers and administrators
/// to investigate and monitor critical system activities.
/// </summary>
public interface ICriticalActionAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    /// <param name="auditLog">The audit log entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(CriticalActionAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user who performed the actions.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs for the user.</returns>
    Task<IReadOnlyList<CriticalActionAuditLog>> GetByUserIdAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by action type.
    /// </summary>
    /// <param name="actionType">The type of critical action.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs for the action type.</returns>
    Task<IReadOnlyList<CriticalActionAuditLog>> GetByActionTypeAsync(
        CriticalActionType actionType,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range with optional filters.
    /// </summary>
    /// <param name="fromDate">Start of the date range (inclusive).</param>
    /// <param name="toDate">End of the date range (inclusive).</param>
    /// <param name="userId">Optional filter by user ID.</param>
    /// <param name="actionType">Optional filter by action type.</param>
    /// <param name="outcome">Optional filter by outcome.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs matching the criteria.</returns>
    Task<IReadOnlyList<CriticalActionAuditLog>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CriticalActionType? actionType = null,
        CriticalActionOutcome? outcome = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific target resource.
    /// </summary>
    /// <param name="targetResourceType">The type of target resource.</param>
    /// <param name="targetResourceId">The ID of the target resource.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs for the resource.</returns>
    Task<IReadOnlyList<CriticalActionAuditLog>> GetByTargetResourceAsync(
        string targetResourceType,
        Guid targetResourceId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts audit logs matching the specified criteria.
    /// </summary>
    /// <param name="fromDate">Start of the date range (inclusive).</param>
    /// <param name="toDate">End of the date range (inclusive).</param>
    /// <param name="userId">Optional filter by user ID.</param>
    /// <param name="actionType">Optional filter by action type.</param>
    /// <param name="outcome">Optional filter by outcome.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of matching audit logs.</returns>
    Task<int> CountAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CriticalActionType? actionType = null,
        CriticalActionOutcome? outcome = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes audit logs that have exceeded their retention period.
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
