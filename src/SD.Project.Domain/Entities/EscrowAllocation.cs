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
    /// Currency code for amounts.
    /// </summary>
    public string Currency { get; private set; } = default!;

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

    /// <summary>
    /// Total amount that has been refunded from this allocation.
    /// Includes both seller amount and shipping amount refunds.
    /// </summary>
    public decimal RefundedAmount { get; private set; }

    /// <summary>
    /// Seller amount (excluding shipping) that has been refunded.
    /// Used to correctly calculate proportional commission refunds.
    /// </summary>
    public decimal RefundedSellerAmount { get; private set; }

    /// <summary>
    /// Commission amount that was refunded (proportional to refund).
    /// Calculated using the original commission rate on the refunded seller amount.
    /// </summary>
    public decimal RefundedCommissionAmount { get; private set; }

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
        string currency,
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

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
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
        Currency = currency.ToUpperInvariant();
        SellerAmount = sellerAmount;
        ShippingAmount = shippingAmount;
        TotalAmount = sellerAmount + shippingAmount;
        CommissionAmount = commissionAmount;
        CommissionRate = commissionRate;
        SellerPayout = sellerAmount - commissionAmount + shippingAmount;
        Status = EscrowAllocationStatus.Held;
        IsEligibleForPayout = false;
        RefundedAmount = 0m;
        RefundedSellerAmount = 0m;
        RefundedCommissionAmount = 0m;
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
    /// Refunds the funds to the buyer (full refund).
    /// </summary>
    public void Refund(string? refundReference = null)
    {
        if (Status != EscrowAllocationStatus.Held)
        {
            throw new InvalidOperationException($"Cannot refund allocation in status {Status}.");
        }

        // Full refund - refund entire amount and commission
        RefundedAmount = TotalAmount;
        RefundedSellerAmount = SellerAmount;
        RefundedCommissionAmount = CommissionAmount;
        Status = EscrowAllocationStatus.Refunded;
        RefundReference = refundReference;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies a partial refund with proportional commission recalculation.
    /// Uses the original commission rate to maintain consistency with historical orders.
    /// </summary>
    /// <param name="refundAmount">The total amount to refund (may include seller amount and/or shipping).</param>
    /// <param name="refundReference">Optional reference ID from refund processing.</param>
    public void ApplyPartialRefund(decimal refundAmount, string? refundReference = null)
    {
        if (Status != EscrowAllocationStatus.Held)
        {
            throw new InvalidOperationException($"Cannot apply partial refund to allocation in status {Status}.");
        }

        if (refundAmount <= 0)
        {
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(refundAmount));
        }

        // Use small tolerance for decimal comparison to handle precision issues
        const decimal tolerance = 0.01m;
        var totalRemainingAmount = TotalAmount - RefundedAmount - refundAmount;
        if (totalRemainingAmount < -tolerance)
        {
            throw new ArgumentException("Refund amount would exceed remaining allocation.", nameof(refundAmount));
        }

        // Calculate how much of the refund applies to seller amount vs shipping
        // Refunds are applied to seller amount first, then shipping
        var remainingSellerAmount = SellerAmount - RefundedSellerAmount;
        var refundFromSellerAmount = Math.Min(refundAmount, remainingSellerAmount);
        
        // Calculate the proportional commission refund using original rate
        // Commission is only on seller amount, not shipping
        var proportionalCommissionRefund = Math.Round(
            refundFromSellerAmount * (CommissionRate / 100m),
            2,
            MidpointRounding.ToEven);

        RefundedAmount += refundAmount;
        RefundedSellerAmount += refundFromSellerAmount;
        RefundedCommissionAmount += proportionalCommissionRefund;
        RefundReference = refundReference;
        UpdatedAt = DateTime.UtcNow;

        // If fully refunded (within tolerance), update status
        if (Math.Abs(RefundedAmount - TotalAmount) <= tolerance || RefundedAmount >= TotalAmount)
        {
            Status = EscrowAllocationStatus.Refunded;
            RefundedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the remaining amount after partial refunds.
    /// </summary>
    public decimal GetRemainingAmount()
    {
        return TotalAmount - RefundedAmount;
    }

    /// <summary>
    /// Gets the remaining commission after partial refunds.
    /// </summary>
    public decimal GetRemainingCommission()
    {
        return CommissionAmount - RefundedCommissionAmount;
    }

    /// <summary>
    /// Gets the remaining seller payout after partial refunds.
    /// SellerPayout = SellerAmount - CommissionAmount + ShippingAmount
    /// After refunds: remaining seller portion + remaining shipping - remaining commission
    /// </summary>
    public decimal GetRemainingSellerPayout()
    {
        var remainingSellerAmount = SellerAmount - RefundedSellerAmount;
        var remainingShipping = ShippingAmount - (RefundedAmount - RefundedSellerAmount);
        var remainingCommission = CommissionAmount - RefundedCommissionAmount;
        
        // Ensure shipping doesn't go negative (all shipping refunded)
        remainingShipping = Math.Max(0, remainingShipping);
        
        return remainingSellerAmount - remainingCommission + remainingShipping;
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

    /// <summary>
    /// Checks if a partial refund can be applied.
    /// Uses small tolerance for decimal precision issues.
    /// </summary>
    public bool CanApplyPartialRefund(decimal refundAmount)
    {
        if (Status != EscrowAllocationStatus.Held)
        {
            return false;
        }

        const decimal tolerance = 0.01m;
        return refundAmount > 0 && (RefundedAmount + refundAmount) <= (TotalAmount + tolerance);
    }
}
