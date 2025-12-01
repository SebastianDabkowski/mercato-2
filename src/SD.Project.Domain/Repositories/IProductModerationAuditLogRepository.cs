using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for ProductModerationAuditLog entities.
/// </summary>
public interface IProductModerationAuditLogRepository
{
    /// <summary>
    /// Gets all audit logs for a specific product.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of audit logs ordered by creation date descending.</returns>
    Task<IReadOnlyCollection<ProductModerationAuditLog>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated audit logs for a specific moderator.
    /// </summary>
    /// <param name="moderatorId">The ID of the moderator.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (items on current page, total count).</returns>
    Task<(IReadOnlyCollection<ProductModerationAuditLog> Items, int TotalCount)> GetByModeratorIdAsync(
        Guid moderatorId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    /// <param name="auditLog">The audit log to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(ProductModerationAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
