namespace SD.Project.Domain.ValueObjects;

/// <summary>
/// Represents the payment status for an order.
/// This is distinct from the overall order status and tracks specifically the payment lifecycle.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending and has not been completed yet.
    /// The buyer has initiated checkout but payment is not confirmed.
    /// </summary>
    Pending,

    /// <summary>
    /// Payment has been successfully completed.
    /// Funds have been received from the buyer.
    /// </summary>
    Paid,

    /// <summary>
    /// Payment has failed.
    /// This can happen due to insufficient funds, declined card, expired authorization, etc.
    /// </summary>
    Failed,

    /// <summary>
    /// Payment has been refunded.
    /// The buyer has received their money back, either partially or fully.
    /// </summary>
    Refunded
}
