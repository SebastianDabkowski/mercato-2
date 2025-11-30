namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of an escrow payment.
/// </summary>
public enum EscrowStatus
{
    /// <summary>Escrow is active and holding funds.</summary>
    Held,
    /// <summary>Escrow has been released to sellers.</summary>
    Released,
    /// <summary>Escrow has been refunded to buyer.</summary>
    Refunded,
    /// <summary>Escrow has been partially released/refunded.</summary>
    PartiallyReleased
}

/// <summary>
/// Represents escrowed payment funds from a buyer.
/// Holds the total payment amount until conditions for release are met.
/// This entity is the main escrow record tied to an order.
/// </summary>
public class EscrowPayment
{
    private readonly List<EscrowAllocation> _allocations = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The order this escrow payment is associated with.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The buyer who made the payment.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// Total amount held in escrow.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Currency code for the escrowed amount.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Current status of the escrow.
    /// </summary>
    public EscrowStatus Status { get; private set; }

    /// <summary>
    /// Payment transaction ID from the payment provider.
    /// </summary>
    public string? PaymentTransactionId { get; private set; }

    /// <summary>
    /// Amount that has been released to sellers.
    /// </summary>
    public decimal ReleasedAmount { get; private set; }

    /// <summary>
    /// Amount that has been refunded to the buyer.
    /// </summary>
    public decimal RefundedAmount { get; private set; }

    /// <summary>
    /// Allocations to individual sellers.
    /// </summary>
    public IReadOnlyCollection<EscrowAllocation> Allocations => _allocations.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    private EscrowPayment()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new escrow payment after successful payment confirmation.
    /// </summary>
    public EscrowPayment(
        Guid orderId,
        Guid buyerId,
        decimal totalAmount,
        string currency,
        string? paymentTransactionId)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (totalAmount <= 0)
        {
            throw new ArgumentException("Total amount must be greater than zero.", nameof(totalAmount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        BuyerId = buyerId;
        TotalAmount = totalAmount;
        Currency = currency.ToUpperInvariant();
        PaymentTransactionId = paymentTransactionId;
        Status = EscrowStatus.Held;
        ReleasedAmount = 0m;
        RefundedAmount = 0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a seller allocation to this escrow.
    /// </summary>
    public EscrowAllocation AddAllocation(
        Guid storeId,
        Guid shipmentId,
        decimal sellerAmount,
        decimal shippingAmount,
        decimal commissionAmount,
        decimal commissionRate)
    {
        if (Status != EscrowStatus.Held)
        {
            throw new InvalidOperationException($"Cannot add allocation to escrow in status {Status}.");
        }

        var existingAllocation = _allocations.FirstOrDefault(a => a.ShipmentId == shipmentId);
        if (existingAllocation is not null)
        {
            throw new InvalidOperationException($"Allocation for shipment {shipmentId} already exists.");
        }

        var allocation = new EscrowAllocation(
            Id,
            storeId,
            shipmentId,
            sellerAmount,
            shippingAmount,
            commissionAmount,
            commissionRate);

        _allocations.Add(allocation);
        UpdatedAt = DateTime.UtcNow;
        return allocation;
    }

    /// <summary>
    /// Releases escrow funds to a specific seller.
    /// Called when a shipment is delivered and payout conditions are met.
    /// </summary>
    public void ReleaseAllocation(Guid shipmentId, string? payoutReference = null)
    {
        if (Status == EscrowStatus.Refunded)
        {
            throw new InvalidOperationException("Cannot release funds from refunded escrow.");
        }

        var allocation = _allocations.FirstOrDefault(a => a.ShipmentId == shipmentId);
        if (allocation is null)
        {
            throw new InvalidOperationException($"Allocation for shipment {shipmentId} not found.");
        }

        allocation.Release(payoutReference);
        ReleasedAmount += allocation.SellerPayout;
        UpdatedAt = DateTime.UtcNow;

        UpdateStatusAfterChange();
    }

    /// <summary>
    /// Refunds the escrow allocation for a specific shipment back to the buyer.
    /// Called when a shipment is cancelled.
    /// </summary>
    public void RefundAllocation(Guid shipmentId, string? refundReference = null)
    {
        if (Status == EscrowStatus.Released)
        {
            throw new InvalidOperationException("Cannot refund funds from fully released escrow.");
        }

        var allocation = _allocations.FirstOrDefault(a => a.ShipmentId == shipmentId);
        if (allocation is null)
        {
            throw new InvalidOperationException($"Allocation for shipment {shipmentId} not found.");
        }

        allocation.Refund(refundReference);
        RefundedAmount += allocation.TotalAmount;
        UpdatedAt = DateTime.UtcNow;

        UpdateStatusAfterChange();
    }

    /// <summary>
    /// Refunds the entire escrow back to the buyer.
    /// Called when the order is cancelled.
    /// </summary>
    public void RefundFull(string? refundReference = null)
    {
        if (Status == EscrowStatus.Released || Status == EscrowStatus.Refunded)
        {
            throw new InvalidOperationException($"Cannot refund escrow in status {Status}.");
        }

        foreach (var allocation in _allocations.Where(a => a.Status == EscrowAllocationStatus.Held))
        {
            allocation.Refund(refundReference);
        }

        RefundedAmount = _allocations.Where(a => a.Status == EscrowAllocationStatus.Refunded)
            .Sum(a => a.TotalAmount);

        Status = _allocations.Any(a => a.Status == EscrowAllocationStatus.Released)
            ? EscrowStatus.PartiallyReleased
            : EscrowStatus.Refunded;

        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Loads allocations from persistence.
    /// </summary>
    public void LoadAllocations(IEnumerable<EscrowAllocation> allocations)
    {
        _allocations.Clear();
        _allocations.AddRange(allocations);
    }

    private void UpdateStatusAfterChange()
    {
        var allReleased = _allocations.All(a => a.Status == EscrowAllocationStatus.Released);
        var allRefunded = _allocations.All(a => a.Status == EscrowAllocationStatus.Refunded);
        var anyReleased = _allocations.Any(a => a.Status == EscrowAllocationStatus.Released);
        var anyRefunded = _allocations.Any(a => a.Status == EscrowAllocationStatus.Refunded);

        if (allReleased)
        {
            Status = EscrowStatus.Released;
            ReleasedAt = DateTime.UtcNow;
        }
        else if (allRefunded)
        {
            Status = EscrowStatus.Refunded;
            RefundedAt = DateTime.UtcNow;
        }
        else if (anyReleased || anyRefunded)
        {
            Status = EscrowStatus.PartiallyReleased;
        }
    }
}
