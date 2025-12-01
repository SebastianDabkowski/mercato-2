namespace SD.Project.Application.Commands;

/// <summary>
/// Command to submit a seller rating after order delivery.
/// Only one rating per order is allowed.
/// </summary>
public record SubmitSellerRatingCommand(
    Guid BuyerId,
    Guid OrderId,
    int Rating,
    string? Comment);
