using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Represents the result of initiating a payment with a provider.
/// </summary>
public sealed record PaymentInitiationResult(
    bool IsSuccess,
    string? TransactionId,
    string? RedirectUrl,
    bool RequiresRedirect,
    bool RequiresBlikCode,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Represents the result of confirming a BLIK payment.
/// </summary>
public sealed record BlikPaymentResult(
    bool IsSuccess,
    string? TransactionId,
    string? ErrorMessage,
    string? ErrorCode);

/// <summary>
/// Represents the result of confirming/verifying a payment with a provider.
/// </summary>
public sealed record PaymentConfirmationResult(
    bool IsSuccess,
    string? TransactionId,
    PaymentConfirmationStatus Status,
    string? ErrorMessage);

/// <summary>
/// Status of a payment confirmation.
/// </summary>
public enum PaymentConfirmationStatus
{
    /// <summary>Payment was successfully completed.</summary>
    Completed,
    /// <summary>Payment is still pending.</summary>
    Pending,
    /// <summary>Payment failed.</summary>
    Failed,
    /// <summary>Payment was cancelled by user.</summary>
    Cancelled,
    /// <summary>Payment expired.</summary>
    Expired
}

/// <summary>
/// Abstraction for integrating with payment providers (e.g., Stripe, PayU, Przelewy24).
/// Provides secure redirect API for card, bank transfer, and BLIK payments.
/// </summary>
public interface IPaymentProviderService
{
    /// <summary>
    /// Initiates a payment with the provider.
    /// Returns a redirect URL for card/bank transfer or indicates BLIK code is needed.
    /// </summary>
    /// <param name="orderId">The order ID for reference.</param>
    /// <param name="amount">The total amount to charge.</param>
    /// <param name="currency">The currency code (e.g., "PLN", "EUR").</param>
    /// <param name="paymentMethodType">The type of payment method.</param>
    /// <param name="idempotencyKey">Unique key to ensure idempotency for retries.</param>
    /// <param name="returnUrl">The URL to return to after payment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing redirect URL or BLIK requirement.</returns>
    Task<PaymentInitiationResult> InitiatePaymentAsync(
        Guid orderId,
        decimal amount,
        string currency,
        PaymentMethodType paymentMethodType,
        string idempotencyKey,
        string returnUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a BLIK code to complete the payment.
    /// </summary>
    /// <param name="orderId">The order ID for reference.</param>
    /// <param name="transactionId">The transaction ID from initial payment initiation.</param>
    /// <param name="blikCode">The 6-digit BLIK code from the user's banking app.</param>
    /// <param name="idempotencyKey">Unique key to ensure idempotency for retries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating if payment was successful.</returns>
    Task<BlikPaymentResult> SubmitBlikCodeAsync(
        Guid orderId,
        string transactionId,
        string blikCode,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms/verifies a payment status with the provider.
    /// Called after user returns from redirect or to check pending payments.
    /// </summary>
    /// <param name="orderId">The order ID for reference.</param>
    /// <param name="transactionId">The transaction ID to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with the current payment status.</returns>
    Task<PaymentConfirmationResult> ConfirmPaymentAsync(
        Guid orderId,
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment method type is enabled for the current environment.
    /// </summary>
    /// <param name="paymentMethodType">The payment method type to check.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    bool IsPaymentMethodEnabled(PaymentMethodType paymentMethodType);
}
