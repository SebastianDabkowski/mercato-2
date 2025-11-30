namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the type of escrow ledger action.
/// </summary>
public enum EscrowLedgerAction
{
    /// <summary>Initial escrow created from payment.</summary>
    Created,
    /// <summary>Allocation added to escrow.</summary>
    AllocationCreated,
    /// <summary>Allocation marked as eligible for payout.</summary>
    AllocationEligible,
    /// <summary>Funds released to seller.</summary>
    Released,
    /// <summary>Funds refunded to buyer.</summary>
    Refunded,
    /// <summary>Partial release to seller.</summary>
    PartialRelease,
    /// <summary>Partial refund to buyer.</summary>
    PartialRefund
}

/// <summary>
/// Immutable ledger entry for escrow audit trail.
/// Every escrow operation creates a ledger entry for full auditability.
/// </summary>
public class EscrowLedger
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The escrow payment this entry relates to.
    /// </summary>
    public Guid EscrowPaymentId { get; private set; }

    /// <summary>
    /// The specific allocation this entry relates to (if applicable).
    /// </summary>
    public Guid? AllocationId { get; private set; }

    /// <summary>
    /// The order this escrow is for.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The store this entry relates to (if applicable).
    /// </summary>
    public Guid? StoreId { get; private set; }

    /// <summary>
    /// The buyer ID.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// The action performed.
    /// </summary>
    public EscrowLedgerAction Action { get; private set; }

    /// <summary>
    /// The amount involved in this action.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Running balance after this action (for the escrow payment).
    /// </summary>
    public decimal BalanceAfter { get; private set; }

    /// <summary>
    /// External reference (payment transaction, payout reference, refund reference).
    /// </summary>
    public string? ExternalReference { get; private set; }

    /// <summary>
    /// Additional notes or details.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Timestamp of the action.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// User or system that initiated the action.
    /// </summary>
    public string? InitiatedBy { get; private set; }

    private EscrowLedger()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new escrow ledger entry.
    /// </summary>
    public EscrowLedger(
        Guid escrowPaymentId,
        Guid? allocationId,
        Guid orderId,
        Guid? storeId,
        Guid buyerId,
        EscrowLedgerAction action,
        decimal amount,
        string currency,
        decimal balanceAfter,
        string? externalReference = null,
        string? notes = null,
        string? initiatedBy = null)
    {
        if (escrowPaymentId == Guid.Empty)
        {
            throw new ArgumentException("Escrow payment ID is required.", nameof(escrowPaymentId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Id = Guid.NewGuid();
        EscrowPaymentId = escrowPaymentId;
        AllocationId = allocationId;
        OrderId = orderId;
        StoreId = storeId;
        BuyerId = buyerId;
        Action = action;
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        BalanceAfter = balanceAfter;
        ExternalReference = externalReference;
        Notes = notes;
        InitiatedBy = initiatedBy;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a ledger entry for escrow creation.
    /// </summary>
    public static EscrowLedger CreateCreatedEntry(
        EscrowPayment escrow,
        string? initiatedBy = null)
    {
        return new EscrowLedger(
            escrow.Id,
            null,
            escrow.OrderId,
            null,
            escrow.BuyerId,
            EscrowLedgerAction.Created,
            escrow.TotalAmount,
            escrow.Currency,
            escrow.TotalAmount,
            escrow.PaymentTransactionId,
            "Escrow created from confirmed payment",
            initiatedBy ?? "System");
    }

    /// <summary>
    /// Creates a ledger entry for allocation creation.
    /// </summary>
    public static EscrowLedger CreateAllocationEntry(
        EscrowPayment escrow,
        EscrowAllocation allocation,
        string? initiatedBy = null)
    {
        return new EscrowLedger(
            escrow.Id,
            allocation.Id,
            escrow.OrderId,
            allocation.StoreId,
            escrow.BuyerId,
            EscrowLedgerAction.AllocationCreated,
            allocation.TotalAmount,
            escrow.Currency,
            escrow.TotalAmount,
            null,
            $"Allocation created for store. Commission: {allocation.CommissionAmount} ({allocation.CommissionRate}%)",
            initiatedBy ?? "System");
    }

    /// <summary>
    /// Creates a ledger entry for marking allocation as eligible.
    /// </summary>
    public static EscrowLedger CreateEligibleEntry(
        EscrowPayment escrow,
        EscrowAllocation allocation,
        string? initiatedBy = null)
    {
        return new EscrowLedger(
            escrow.Id,
            allocation.Id,
            escrow.OrderId,
            allocation.StoreId,
            escrow.BuyerId,
            EscrowLedgerAction.AllocationEligible,
            allocation.SellerPayout,
            escrow.Currency,
            escrow.TotalAmount - escrow.ReleasedAmount - escrow.RefundedAmount,
            null,
            "Allocation marked eligible for payout after delivery confirmation",
            initiatedBy ?? "System");
    }

    /// <summary>
    /// Creates a ledger entry for funds release.
    /// </summary>
    public static EscrowLedger CreateReleaseEntry(
        EscrowPayment escrow,
        EscrowAllocation allocation,
        string? payoutReference,
        string? initiatedBy = null)
    {
        return new EscrowLedger(
            escrow.Id,
            allocation.Id,
            escrow.OrderId,
            allocation.StoreId,
            escrow.BuyerId,
            EscrowLedgerAction.Released,
            allocation.SellerPayout,
            escrow.Currency,
            escrow.TotalAmount - escrow.ReleasedAmount - escrow.RefundedAmount,
            payoutReference,
            $"Funds released to seller. Commission retained: {allocation.CommissionAmount}",
            initiatedBy ?? "System");
    }

    /// <summary>
    /// Creates a ledger entry for refund.
    /// </summary>
    public static EscrowLedger CreateRefundEntry(
        EscrowPayment escrow,
        EscrowAllocation allocation,
        string? refundReference,
        string? initiatedBy = null)
    {
        return new EscrowLedger(
            escrow.Id,
            allocation.Id,
            escrow.OrderId,
            allocation.StoreId,
            escrow.BuyerId,
            EscrowLedgerAction.Refunded,
            allocation.TotalAmount,
            escrow.Currency,
            escrow.TotalAmount - escrow.ReleasedAmount - escrow.RefundedAmount,
            refundReference,
            "Funds refunded to buyer",
            initiatedBy ?? "System");
    }
}
