using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Services;

/// <summary>
/// Maps external payment provider status codes to internal PaymentStatus values.
/// All provider status code mappings are centralized here to maintain consistency
/// and allow easy extension when adding new payment providers.
/// </summary>
public static class PaymentStatusMapper
{
    /// <summary>
    /// Maps a provider status code to an internal PaymentStatus.
    /// </summary>
    /// <param name="providerStatusCode">The status code from the payment provider.</param>
    /// <param name="providerName">Optional provider name for provider-specific mappings.</param>
    /// <returns>The corresponding internal PaymentStatus.</returns>
    public static PaymentStatus MapFromProviderStatus(string providerStatusCode, string? providerName = null)
    {
        if (string.IsNullOrWhiteSpace(providerStatusCode))
        {
            return PaymentStatus.Pending;
        }

        // Normalize the status code for consistent matching
        var normalizedCode = providerStatusCode.Trim().ToUpperInvariant();

        // Provider-specific mappings
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            var providerNormalized = providerName.Trim().ToUpperInvariant();
            
            // Stripe status codes
            if (providerNormalized == "STRIPE")
            {
                return MapStripeStatus(normalizedCode);
            }

            // PayU status codes
            if (providerNormalized == "PAYU")
            {
                return MapPayUStatus(normalizedCode);
            }

            // Przelewy24 status codes
            if (providerNormalized == "P24" || providerNormalized == "PRZELEWY24")
            {
                return MapPrzelewy24Status(normalizedCode);
            }
        }

        // Generic/common status code mappings
        return MapGenericStatus(normalizedCode);
    }

    /// <summary>
    /// Gets a user-friendly display message for a payment status.
    /// Messages are designed to be clear and non-technical.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>A user-friendly message describing the payment status.</returns>
    public static string GetBuyerFriendlyMessage(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Your payment is being processed. This may take a few moments.",
            PaymentStatus.Paid => "Payment successful! Your order has been confirmed.",
            PaymentStatus.Failed => "We couldn't process your payment. Please try again or use a different payment method.",
            PaymentStatus.Refunded => "Your payment has been refunded. The funds will be returned to your original payment method.",
            _ => "Payment status is being updated."
        };
    }

    /// <summary>
    /// Gets a detailed message for failed payments that is appropriate for buyers.
    /// This method intentionally does not expose technical error details.
    /// </summary>
    /// <param name="providerErrorCode">The error code from the provider (used for logging only).</param>
    /// <returns>A user-friendly error message.</returns>
    public static string GetFailureMessageForBuyer(string? providerErrorCode = null)
    {
        // Note: providerErrorCode should be logged for debugging but not exposed to user
        // All buyer-facing messages are intentionally generic to avoid exposing technical details
        return "We couldn't complete your payment. Please check your payment details and try again, or choose a different payment method.";
    }

    private static PaymentStatus MapStripeStatus(string statusCode)
    {
        return statusCode switch
        {
            // Stripe payment intent statuses
            "SUCCEEDED" => PaymentStatus.Paid,
            "PROCESSING" => PaymentStatus.Pending,
            "REQUIRES_PAYMENT_METHOD" => PaymentStatus.Pending,
            "REQUIRES_CONFIRMATION" => PaymentStatus.Pending,
            "REQUIRES_ACTION" => PaymentStatus.Pending,
            "REQUIRES_CAPTURE" => PaymentStatus.Pending,
            "CANCELED" => PaymentStatus.Failed,
            "PAYMENT_FAILED" => PaymentStatus.Failed,
            
            // Stripe refund statuses
            "REFUNDED" => PaymentStatus.Refunded,
            "PARTIALLY_REFUNDED" => PaymentStatus.Refunded,
            
            // Fallback to generic mapping
            _ => MapGenericStatus(statusCode)
        };
    }

    private static PaymentStatus MapPayUStatus(string statusCode)
    {
        return statusCode switch
        {
            // PayU order statuses
            "COMPLETED" => PaymentStatus.Paid,
            "WAITING_FOR_CONFIRMATION" => PaymentStatus.Pending,
            "PENDING" => PaymentStatus.Pending,
            "NEW" => PaymentStatus.Pending,
            "CANCELED" => PaymentStatus.Failed,
            "REJECTED" => PaymentStatus.Failed,
            
            // PayU refund statuses
            "REFUND" => PaymentStatus.Refunded,
            "REFUND_PENDING" => PaymentStatus.Refunded,
            
            // Fallback to generic mapping
            _ => MapGenericStatus(statusCode)
        };
    }

    private static PaymentStatus MapPrzelewy24Status(string statusCode)
    {
        return statusCode switch
        {
            // Przelewy24 statuses
            "TRUE" => PaymentStatus.Paid,  // Verification success
            "1" => PaymentStatus.Paid,     // Payment successful
            "VERIFIED" => PaymentStatus.Paid,
            "WAITING" => PaymentStatus.Pending,
            "0" => PaymentStatus.Pending,
            "FALSE" => PaymentStatus.Failed,
            "ERROR" => PaymentStatus.Failed,
            "-1" => PaymentStatus.Failed,
            
            // Fallback to generic mapping
            _ => MapGenericStatus(statusCode)
        };
    }

    private static PaymentStatus MapGenericStatus(string statusCode)
    {
        return statusCode switch
        {
            // Common success indicators
            "SUCCESS" or "SUCCESSFUL" or "SUCCEEDED" or "COMPLETED" or "PAID" 
                or "APPROVED" or "AUTHORIZED" or "CAPTURED" or "SETTLED" => PaymentStatus.Paid,
            
            // Common pending indicators
            "PENDING" or "PROCESSING" or "IN_PROGRESS" or "WAITING" 
                or "INITIATED" or "CREATED" or "NEW" => PaymentStatus.Pending,
            
            // Common failure indicators
            "FAILED" or "FAILURE" or "DECLINED" or "REJECTED" or "CANCELLED" 
                or "CANCELED" or "EXPIRED" or "ERROR" or "DENIED" 
                or "INSUFFICIENT_FUNDS" or "CARD_DECLINED" => PaymentStatus.Failed,
            
            // Refund indicators
            "REFUNDED" or "REFUND" or "PARTIALLY_REFUNDED" or "CHARGEBACK" 
                or "REVERSED" or "VOIDED" => PaymentStatus.Refunded,
            
            // Default to pending for unknown statuses (safest assumption)
            _ => PaymentStatus.Pending
        };
    }
}
