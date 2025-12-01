using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for review moderation audit log persistence operations.
/// </summary>
public interface IReviewModerationAuditLogRepository
{
    /// <summary>
    /// Gets audit logs for a specific review.
    /// </summary>
    Task<IReadOnlyList<ReviewModerationAuditLog>> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated audit logs with optional filters.
    /// </summary>
    Task<(IReadOnlyList<ReviewModerationAuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? reviewId,
        Guid? moderatorId,
        ReviewModerationAction? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddAsync(ReviewModerationAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
