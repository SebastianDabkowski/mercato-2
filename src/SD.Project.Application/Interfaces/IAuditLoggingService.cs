using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Service for logging access to sensitive data and critical actions for audit and compliance purposes.
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Logs access to a sensitive resource.
    /// </summary>
    /// <param name="accessedByUserId">The ID of the user accessing the data.</param>
    /// <param name="accessedByRole">The role of the user accessing the data.</param>
    /// <param name="resourceType">The type of sensitive resource being accessed.</param>
    /// <param name="resourceId">The unique identifier of the resource.</param>
    /// <param name="action">The action performed on the resource.</param>
    /// <param name="resourceOwnerId">The ID of the data owner (optional).</param>
    /// <param name="accessReason">Additional context for the access (optional).</param>
    /// <param name="ipAddress">The IP address of the accessor (optional).</param>
    /// <param name="userAgent">The user agent string (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogSensitiveAccessAsync(
        Guid accessedByUserId,
        UserRole accessedByRole,
        SensitiveResourceType resourceType,
        Guid resourceId,
        SensitiveAccessAction action,
        Guid? resourceOwnerId = null,
        string? accessReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific resource.
    /// </summary>
    Task<IReadOnlyList<SensitiveAccessAuditLog>> GetAuditLogsForResourceAsync(
        SensitiveResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for access made by a specific user.
    /// </summary>
    Task<IReadOnlyList<SensitiveAccessAuditLog>> GetAuditLogsByAccessorAsync(
        Guid accessedByUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a critical action in the system.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="userRole">The role of the user at the time of the action.</param>
    /// <param name="actionType">The type of critical action.</param>
    /// <param name="targetResourceType">The type of resource affected.</param>
    /// <param name="targetResourceId">The ID of the target resource (optional).</param>
    /// <param name="outcome">The outcome of the action.</param>
    /// <param name="details">Additional details about the action (optional).</param>
    /// <param name="ipAddress">The IP address of the client (optional).</param>
    /// <param name="userAgent">The user agent string (optional).</param>
    /// <param name="correlationId">Correlation ID for tracing (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogCriticalActionAsync(
        Guid userId,
        UserRole userRole,
        CriticalActionType actionType,
        string targetResourceType,
        Guid? targetResourceId,
        CriticalActionOutcome outcome,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets critical action audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user who performed the actions.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit logs for the user.</returns>
    Task<IReadOnlyList<CriticalActionAuditLog>> GetCriticalActionLogsByUserAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets critical action audit logs within a date range with optional filters.
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
    Task<IReadOnlyList<CriticalActionAuditLog>> GetCriticalActionLogsAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CriticalActionType? actionType = null,
        CriticalActionOutcome? outcome = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);
}
