namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a single item (escrow allocation) included in a settlement.
/// Provides the order-level detail for drilling into settlement data.
/// </summary>
public class SettlementItem
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The parent settlement this item belongs to.
    /// </summary>
    public Guid SettlementId { get; private set; }

    /// <summary>
    /// The escrow allocation this item represents.
    /// </summary>
    public Guid EscrowAllocationId { get; private set; }

    /// <summary>
    /// The shipment (sub-order) this item is for.
    /// </summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>
    /// The order number for display.
    /// </summary>
    public string? OrderNumber { get; private set; }

    /// <summary>
    /// The seller's portion of the order amount (excluding shipping).
    /// </summary>
    public decimal SellerAmount { get; private set; }

    /// <summary>
    /// The shipping cost for this item.
    /// </summary>
    public decimal ShippingAmount { get; private set; }

    /// <summary>
    /// The commission amount deducted.
    /// </summary>
    public decimal CommissionAmount { get; private set; }

    /// <summary>
    /// Any refunded amount for this item.
    /// </summary>
    public decimal RefundedAmount { get; private set; }

    /// <summary>
    /// Net amount for this item.
    /// Calculated as: SellerAmount + ShippingAmount - CommissionAmount - RefundedAmount
    /// </summary>
    public decimal NetAmount { get; private set; }

    /// <summary>
    /// Date of the transaction (when escrow was created/updated).
    /// </summary>
    public DateTime TransactionDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private SettlementItem()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new settlement item.
    /// </summary>
    public SettlementItem(
        Guid settlementId,
        Guid escrowAllocationId,
        Guid shipmentId,
        string? orderNumber,
        decimal sellerAmount,
        decimal shippingAmount,
        decimal commissionAmount,
        decimal refundedAmount,
        DateTime transactionDate)
    {
        if (settlementId == Guid.Empty)
        {
            throw new ArgumentException("Settlement ID is required.", nameof(settlementId));
        }

        if (escrowAllocationId == Guid.Empty)
        {
            throw new ArgumentException("Escrow allocation ID is required.", nameof(escrowAllocationId));
        }

        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        Id = Guid.NewGuid();
        SettlementId = settlementId;
        EscrowAllocationId = escrowAllocationId;
        ShipmentId = shipmentId;
        OrderNumber = orderNumber;
        SellerAmount = sellerAmount;
        ShippingAmount = shippingAmount;
        CommissionAmount = commissionAmount;
        RefundedAmount = refundedAmount;
        TransactionDate = transactionDate;
        NetAmount = sellerAmount + shippingAmount - commissionAmount - refundedAmount;
        CreatedAt = DateTime.UtcNow;
    }
}
