namespace SD.Project.Application.Interfaces;

/// <summary>
/// Represents the result of a refund operation with a payment provider.
/// </summary>
public sealed record RefundProviderResult(
    bool IsSuccess,
    string? RefundTransactionId,
    RefundProviderStatus Status,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Status of a refund operation from the payment provider.
/// </summary>
public enum RefundProviderStatus
{
    /// <summary>Refund was successfully processed.</summary>
    Completed,
    /// <summary>Refund is pending (async processing).</summary>
    Pending,
    /// <summary>Refund failed.</summary>
    Failed,
    /// <summary>Refund was rejected by the provider.</summary>
    Rejected
}

/// <summary>
/// Abstraction for integrating with payment providers to process refunds.
/// Handles communication with external payment providers for refund operations.
/// </summary>
public interface IRefundProviderService
{
    /// <summary>
    /// Processes a full refund for a payment transaction.
    /// </summary>
    /// <param name="orderId">The order ID for reference.</param>
    /// <param name="originalTransactionId">The original payment transaction ID.</param>
    /// <param name="amount">The amount to refund.</param>
    /// <param name="currency">The currency code (e.g., "PLN", "EUR").</param>
    /// <param name="idempotencyKey">Unique key to ensure idempotency for retries.</param>
    /// <param name="reason">Optional reason for the refund.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing refund status and transaction details.</returns>
    Task<RefundProviderResult> ProcessFullRefundAsync(
        Guid orderId,
        string originalTransactionId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a partial refund for a payment transaction.
    /// </summary>
    /// <param name="orderId">The order ID for reference.</param>
    /// <param name="originalTransactionId">The original payment transaction ID.</param>
    /// <param name="refundAmount">The amount to refund (partial).</param>
    /// <param name="currency">The currency code (e.g., "PLN", "EUR").</param>
    /// <param name="idempotencyKey">Unique key to ensure idempotency for retries.</param>
    /// <param name="reason">Optional reason for the refund.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing refund status and transaction details.</returns>
    Task<RefundProviderResult> ProcessPartialRefundAsync(
        Guid orderId,
        string originalTransactionId,
        decimal refundAmount,
        string currency,
        string idempotencyKey,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a pending refund.
    /// </summary>
    /// <param name="refundTransactionId">The refund transaction ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with the current refund status.</returns>
    Task<RefundProviderResult> GetRefundStatusAsync(
        string refundTransactionId,
        CancellationToken cancellationToken = default);
}
