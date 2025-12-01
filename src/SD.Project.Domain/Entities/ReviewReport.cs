namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the reason for reporting a review.
/// </summary>
public enum ReviewReportReason
{
    /// <summary>Abusive or offensive content.</summary>
    Abuse,
    /// <summary>Spam or promotional content.</summary>
    Spam,
    /// <summary>Contains false or misleading information.</summary>
    FalseInformation,
    /// <summary>Other reason not covered by the above categories.</summary>
    Other
}

/// <summary>
/// Represents a report submitted by a buyer against a review.
/// </summary>
public class ReviewReport
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The review being reported.
    /// </summary>
    public Guid ReviewId { get; private set; }

    /// <summary>
    /// The user who submitted the report.
    /// </summary>
    public Guid ReporterId { get; private set; }

    /// <summary>
    /// The reason for the report.
    /// </summary>
    public ReviewReportReason Reason { get; private set; }

    /// <summary>
    /// Optional additional details provided by the reporter.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Timestamp when the report was submitted.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private ReviewReport()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new review report.
    /// </summary>
    public ReviewReport(
        Guid reviewId,
        Guid reporterId,
        ReviewReportReason reason,
        string? details = null)
    {
        if (reviewId == Guid.Empty)
        {
            throw new ArgumentException("Review ID is required.", nameof(reviewId));
        }

        if (reporterId == Guid.Empty)
        {
            throw new ArgumentException("Reporter ID is required.", nameof(reporterId));
        }

        Id = Guid.NewGuid();
        ReviewId = reviewId;
        ReporterId = reporterId;
        Reason = reason;
        Details = details?.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
