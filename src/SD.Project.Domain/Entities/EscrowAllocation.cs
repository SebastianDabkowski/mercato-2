namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of an escrow allocation for a specific seller.
/// </summary>
public enum EscrowAllocationStatus
{
    /// <summary>Allocation is held in escrow.</summary>
    Held,
    /// <summary>Allocation has been released to the seller.</summary>
    Released,
    /// <summary>Allocation has been refunded to the buyer.</summary>
    Refunded
}

/// <summary>
/// Represents a seller's portion of an escrow payment.
/// Tracks the amount allocated to a specific seller including commission deduction.
/// </summary>
public class EscrowAllocation
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The parent escrow payment.
    /// </summary>
    public Guid EscrowPaymentId { get; private set; }

    /// <summary>
    /// The seller's store ID.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The shipment (sub-order) this allocation is for.
    /// </summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>
    /// The seller's portion of the order amount (excluding shipping).
    /// </summary>
    public decimal SellerAmount { get; private set; }

    /// <summary>
    /// The shipping cost collected for this seller's items.
    /// </summary>
    public decimal ShippingAmount { get; private set; }

    /// <summary>
    /// Total amount allocated (seller amount + shipping).
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Platform commission amount deducted from seller payout.
    /// </summary>
    public decimal CommissionAmount { get; private set; }

    /// <summary>
    /// Commission rate applied (percentage, e.g., 10 for 10%).
    /// </summary>
    public decimal CommissionRate { get; private set; }

    /// <summary>
    /// Amount to be paid out to the seller (SellerAmount - CommissionAmount).
    /// Shipping is passed through to the seller.
    /// </summary>
    public decimal SellerPayout { get; private set; }

    /// <summary>
    /// Current status of this allocation.
    /// </summary>
    public EscrowAllocationStatus Status { get; private set; }

    /// <summary>
    /// Whether this allocation is eligible for payout.
    /// Configured based on business rules (e.g., delivery confirmation, time elapsed).
    /// </summary>
    public bool IsEligibleForPayout { get; private set; }

    /// <summary>
    /// Reference ID from payout provider when funds are released.
    /// </summary>
    public string? PayoutReference { get; private set; }

    /// <summary>
    /// Reference ID from refund processing when funds are refunded.
    /// </summary>
    public string? RefundReference { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? PayoutEligibleAt { get; private set; }

    private EscrowAllocation()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new escrow allocation for a seller.
    /// </summary>
    public EscrowAllocation(
        Guid escrowPaymentId,
        Guid storeId,
        Guid shipmentId,
        decimal sellerAmount,
        decimal shippingAmount,
        decimal commissionAmount,
        decimal commissionRate)
    {
        if (escrowPaymentId == Guid.Empty)
        {
            throw new ArgumentException("Escrow payment ID is required.", nameof(escrowPaymentId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        if (sellerAmount < 0)
        {
            throw new ArgumentException("Seller amount cannot be negative.", nameof(sellerAmount));
        }

        if (shippingAmount < 0)
        {
            throw new ArgumentException("Shipping amount cannot be negative.", nameof(shippingAmount));
        }

        if (commissionAmount < 0)
        {
            throw new ArgumentException("Commission amount cannot be negative.", nameof(commissionAmount));
        }

        if (commissionRate < 0 || commissionRate > 100)
        {
            throw new ArgumentException("Commission rate must be between 0 and 100.", nameof(commissionRate));
        }

        Id = Guid.NewGuid();
        EscrowPaymentId = escrowPaymentId;
        StoreId = storeId;
        ShipmentId = shipmentId;
        SellerAmount = sellerAmount;
        ShippingAmount = shippingAmount;
        TotalAmount = sellerAmount + shippingAmount;
        CommissionAmount = commissionAmount;
        CommissionRate = commissionRate;
        SellerPayout = sellerAmount - commissionAmount + shippingAmount;
        Status = EscrowAllocationStatus.Held;
        IsEligibleForPayout = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this allocation as eligible for payout.
    /// Called when payout conditions are met (e.g., delivery confirmed).
    /// </summary>
    public void MarkEligibleForPayout()
    {
        if (Status != EscrowAllocationStatus.Held)
        {
            throw new InvalidOperationException($"Cannot mark allocation as eligible in status {Status}.");
        }

        IsEligibleForPayout = true;
        PayoutEligibleAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Releases the funds to the seller.
    /// </summary>
    public void Release(string? payoutReference = null)
    {
        if (Status != EscrowAllocationStatus.Held)
        {
            throw new InvalidOperationException($"Cannot release allocation in status {Status}.");
        }

        Status = EscrowAllocationStatus.Released;
        PayoutReference = payoutReference;
        ReleasedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refunds the funds to the buyer.
    /// </summary>
    public void Refund(string? refundReference = null)
    {
        if (Status != EscrowAllocationStatus.Held)
        {
            throw new InvalidOperationException($"Cannot refund allocation in status {Status}.");
        }

        Status = EscrowAllocationStatus.Refunded;
        RefundReference = refundReference;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the allocation can be released.
    /// </summary>
    public bool CanBeReleased()
    {
        return Status == EscrowAllocationStatus.Held && IsEligibleForPayout;
    }

    /// <summary>
    /// Checks if the allocation can be refunded.
    /// </summary>
    public bool CanBeRefunded()
    {
        return Status == EscrowAllocationStatus.Held;
    }
}
