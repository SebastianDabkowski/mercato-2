using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Service for logging access to sensitive data for audit and compliance purposes.
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
}
