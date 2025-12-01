using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for review report persistence operations.
/// </summary>
public interface IReviewReportRepository
{
    /// <summary>
    /// Gets a report by ID.
    /// </summary>
    Task<ReviewReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a report by review ID and reporter ID.
    /// Used to check if a user has already reported a review.
    /// </summary>
    Task<ReviewReport?> GetByReviewAndReporterAsync(
        Guid reviewId,
        Guid reporterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reports for a specific review.
    /// </summary>
    Task<IReadOnlyList<ReviewReport>> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new review report.
    /// </summary>
    Task AddAsync(ReviewReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
