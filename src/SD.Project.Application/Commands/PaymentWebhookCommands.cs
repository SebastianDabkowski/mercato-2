namespace SD.Project.Application.Commands;

/// <summary>
/// Command to process a payment webhook notification from a payment provider.
/// </summary>
/// <param name="OrderId">The order ID associated with this payment.</param>
/// <param name="TransactionId">The transaction ID from the payment provider.</param>
/// <param name="ProviderStatusCode">The status code from the payment provider.</param>
/// <param name="ProviderName">The name of the payment provider (e.g., "Stripe", "PayU").</param>
/// <param name="ProviderSignature">Signature for webhook verification.</param>
/// <param name="RawPayload">The raw webhook payload for logging.</param>
public sealed record ProcessPaymentWebhookCommand(
    Guid OrderId,
    string TransactionId,
    string ProviderStatusCode,
    string? ProviderName = null,
    string? ProviderSignature = null,
    string? RawPayload = null);

/// <summary>
/// Command to update payment status based on provider callback.
/// </summary>
/// <param name="OrderId">The order ID to update.</param>
/// <param name="TransactionId">The transaction ID from the provider.</param>
/// <param name="ProviderStatusCode">The provider's status code.</param>
/// <param name="ProviderName">The payment provider name.</param>
/// <param name="RefundAmount">Optional refund amount if this is a refund notification.</param>
public sealed record UpdatePaymentStatusCommand(
    Guid OrderId,
    string TransactionId,
    string ProviderStatusCode,
    string? ProviderName = null,
    decimal? RefundAmount = null);
