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
