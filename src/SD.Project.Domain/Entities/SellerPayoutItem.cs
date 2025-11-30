namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a single escrow allocation included in a seller payout.
/// Links the payout to the individual escrow allocations being paid out.
/// </summary>
public class SellerPayoutItem
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The parent payout this item belongs to.
    /// </summary>
    public Guid SellerPayoutId { get; private set; }

    /// <summary>
    /// The escrow allocation being paid out.
    /// </summary>
    public Guid EscrowAllocationId { get; private set; }

    /// <summary>
    /// The amount being paid out for this allocation.
    /// </summary>
    public decimal Amount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private SellerPayoutItem()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new payout item.
    /// </summary>
    public SellerPayoutItem(Guid sellerPayoutId, Guid escrowAllocationId, decimal amount)
    {
        if (sellerPayoutId == Guid.Empty)
        {
            throw new ArgumentException("Seller payout ID is required.", nameof(sellerPayoutId));
        }

        if (escrowAllocationId == Guid.Empty)
        {
            throw new ArgumentException("Escrow allocation ID is required.", nameof(escrowAllocationId));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        Id = Guid.NewGuid();
        SellerPayoutId = sellerPayoutId;
        EscrowAllocationId = escrowAllocationId;
        Amount = amount;
        CreatedAt = DateTime.UtcNow;
    }
}
