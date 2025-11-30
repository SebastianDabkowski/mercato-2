namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of processing a payment webhook.
/// </summary>
/// <param name="Success">Whether the webhook was processed successfully.</param>
/// <param name="Message">A message describing the result.</param>
/// <param name="PreviousPaymentStatus">The payment status before the update.</param>
/// <param name="NewPaymentStatus">The payment status after the update.</param>
public sealed record PaymentWebhookResultDto(
    bool Success,
    string? Message = null,
    string? PreviousPaymentStatus = null,
    string? NewPaymentStatus = null)
{
    public static PaymentWebhookResultDto Succeeded(string previousStatus, string newStatus) =>
        new(true, "Payment status updated successfully.", previousStatus, newStatus);

    public static PaymentWebhookResultDto Failed(string message) =>
        new(false, message);

    public static PaymentWebhookResultDto NoChange(string currentStatus) =>
        new(true, "No status change required.", currentStatus, currentStatus);
}

/// <summary>
/// DTO containing payment status information for display.
/// </summary>
/// <param name="Status">The payment status.</param>
/// <param name="DisplayMessage">User-friendly message describing the status.</param>
/// <param name="IsPaid">Whether the payment was successful.</param>
/// <param name="IsFailed">Whether the payment failed.</param>
/// <param name="IsRefunded">Whether the payment was refunded.</param>
/// <param name="IsPending">Whether the payment is still pending.</param>
/// <param name="RefundedAmount">The refunded amount if applicable.</param>
/// <param name="FailureMessage">A user-friendly failure message if payment failed.</param>
public sealed record PaymentStatusDto(
    string Status,
    string DisplayMessage,
    bool IsPaid,
    bool IsFailed,
    bool IsRefunded,
    bool IsPending,
    decimal? RefundedAmount = null,
    string? FailureMessage = null);
