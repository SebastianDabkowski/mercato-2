namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a refund request.
/// </summary>
public enum RefundStatus
{
    /// <summary>Refund has been requested and is awaiting processing.</summary>
    Pending,
    /// <summary>Refund is being processed by the payment provider.</summary>
    Processing,
    /// <summary>Refund has been successfully completed.</summary>
    Completed,
    /// <summary>Refund has failed.</summary>
    Failed,
    /// <summary>Refund was rejected (e.g., by business rules or provider).</summary>
    Rejected
}

/// <summary>
/// Represents the type of refund.
/// </summary>
public enum RefundType
{
    /// <summary>Full refund of the entire order amount.</summary>
    Full,
    /// <summary>Partial refund of a specific amount.</summary>
    Partial
}

/// <summary>
/// Represents a refund request for an order or shipment.
/// Tracks the lifecycle of a refund from request to completion.
/// Provides full audit trail for compliance and investigation.
/// </summary>
public class Refund
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The order this refund is associated with.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The shipment this refund is associated with (for partial/shipment-level refunds).
    /// </summary>
    public Guid? ShipmentId { get; private set; }

    /// <summary>
    /// The buyer who will receive the refund.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// The store/seller involved in this refund (for seller-initiated refunds).
    /// </summary>
    public Guid? StoreId { get; private set; }

    /// <summary>
    /// Type of refund (full or partial).
    /// </summary>
    public RefundType Type { get; private set; }

    /// <summary>
    /// Current status of the refund.
    /// </summary>
    public RefundStatus Status { get; private set; }

    /// <summary>
    /// The amount being refunded.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code for the refund amount.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// The commission amount being refunded (proportional).
    /// </summary>
    public decimal CommissionRefundAmount { get; private set; }

    /// <summary>
    /// Reason for the refund (required for auditing).
    /// </summary>
    public string Reason { get; private set; } = default!;

    /// <summary>
    /// The original payment transaction ID.
    /// </summary>
    public string? OriginalTransactionId { get; private set; }

    /// <summary>
    /// The refund transaction ID from the payment provider.
    /// </summary>
    public string? RefundTransactionId { get; private set; }

    /// <summary>
    /// Idempotency key for the refund request.
    /// </summary>
    public string IdempotencyKey { get; private set; } = default!;

    /// <summary>
    /// ID of the user who initiated the refund (support agent or seller).
    /// </summary>
    public Guid InitiatedById { get; private set; }

    /// <summary>
    /// Type of initiator (SupportAgent, Seller).
    /// </summary>
    public string InitiatorType { get; private set; } = default!;

    /// <summary>
    /// Error message if the refund failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Error code from the payment provider.
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Number of retry attempts for failed refunds.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Maximum number of retries allowed.
    /// </summary>
    public const int MaxRetries = 3;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Refund()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new refund request.
    /// </summary>
    public Refund(
        Guid orderId,
        Guid? shipmentId,
        Guid buyerId,
        Guid? storeId,
        RefundType type,
        decimal amount,
        string currency,
        decimal commissionRefundAmount,
        string reason,
        string? originalTransactionId,
        Guid initiatedById,
        string initiatorType)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Refund reason is required.", nameof(reason));
        }

        if (initiatedById == Guid.Empty)
        {
            throw new ArgumentException("Initiator ID is required.", nameof(initiatedById));
        }

        if (string.IsNullOrWhiteSpace(initiatorType))
        {
            throw new ArgumentException("Initiator type is required.", nameof(initiatorType));
        }

        if (commissionRefundAmount < 0)
        {
            throw new ArgumentException("Commission refund amount cannot be negative.", nameof(commissionRefundAmount));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        ShipmentId = shipmentId;
        BuyerId = buyerId;
        StoreId = storeId;
        Type = type;
        Status = RefundStatus.Pending;
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        CommissionRefundAmount = commissionRefundAmount;
        Reason = reason.Trim();
        OriginalTransactionId = originalTransactionId;
        IdempotencyKey = $"REFUND-{orderId}-{Guid.NewGuid():N}";
        InitiatedById = initiatedById;
        InitiatorType = initiatorType;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the refund as processing.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != RefundStatus.Pending && Status != RefundStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot start processing refund in status {Status}.");
        }

        Status = RefundStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the refund as completed.
    /// </summary>
    public void Complete(string refundTransactionId)
    {
        if (Status != RefundStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete refund in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(refundTransactionId))
        {
            throw new ArgumentException("Refund transaction ID is required.", nameof(refundTransactionId));
        }

        Status = RefundStatus.Completed;
        RefundTransactionId = refundTransactionId;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the refund as failed.
    /// </summary>
    public void Fail(string? errorMessage, string? errorCode)
    {
        if (Status != RefundStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot fail refund in status {Status}.");
        }

        Status = RefundStatus.Failed;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the refund as rejected.
    /// </summary>
    public void Reject(string reason)
    {
        if (Status == RefundStatus.Completed)
        {
            throw new InvalidOperationException("Cannot reject completed refund.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        Status = RefundStatus.Rejected;
        ErrorMessage = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the refund can be retried.
    /// </summary>
    public bool CanRetry()
    {
        return Status == RefundStatus.Failed && RetryCount < MaxRetries;
    }

    /// <summary>
    /// Resets the refund for retry.
    /// </summary>
    public void ResetForRetry()
    {
        if (!CanRetry())
        {
            throw new InvalidOperationException("Cannot retry refund. Max retries exceeded or wrong status.");
        }

        Status = RefundStatus.Pending;
        ErrorMessage = null;
        ErrorCode = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
