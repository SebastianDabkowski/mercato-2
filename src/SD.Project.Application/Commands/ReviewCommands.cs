using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to submit a product review after order delivery.
/// </summary>
public record SubmitReviewCommand(
    Guid BuyerId,
    Guid OrderId,
    Guid ShipmentId,
    Guid ProductId,
    int Rating,
    string? Comment);

/// <summary>
/// Command to report a review for admin review.
/// </summary>
public record ReportReviewCommand(
    Guid ReviewId,
    Guid ReporterId,
    ReviewReportReason Reason,
    string? Details = null);
