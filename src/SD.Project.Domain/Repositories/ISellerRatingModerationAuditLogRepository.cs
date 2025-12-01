using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for seller rating moderation audit log persistence operations.
/// </summary>
public interface ISellerRatingModerationAuditLogRepository
{
    /// <summary>
    /// Gets audit logs for a specific seller rating.
    /// </summary>
    Task<IReadOnlyList<SellerRatingModerationAuditLog>> GetBySellerRatingIdAsync(
        Guid sellerRatingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated audit logs with optional filters.
    /// </summary>
    Task<(IReadOnlyList<SellerRatingModerationAuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? sellerRatingId,
        Guid? moderatorId,
        SellerRatingModerationAction? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddAsync(SellerRatingModerationAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
