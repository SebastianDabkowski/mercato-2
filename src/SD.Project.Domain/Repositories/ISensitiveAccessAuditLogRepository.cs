using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for sensitive data access audit logs.
/// </summary>
public interface ISensitiveAccessAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddAsync(SensitiveAccessAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific resource.
    /// </summary>
    Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByResourceAsync(
        SensitiveResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for accesses made by a specific user.
    /// </summary>
    Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByAccessorAsync(
        Guid accessedByUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific resource owner's data.
    /// </summary>
    Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByResourceOwnerAsync(
        Guid resourceOwnerId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range.
    /// </summary>
    Task<IReadOnlyList<SensitiveAccessAuditLog>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        SensitiveResourceType? resourceType = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
