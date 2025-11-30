namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an adjustment to a settlement for a previous period.
/// Used to correct errors or account for late transactions.
/// </summary>
public class SettlementAdjustment
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The settlement this adjustment belongs to.
    /// </summary>
    public Guid SettlementId { get; private set; }

    /// <summary>
    /// The year of the original period being adjusted.
    /// </summary>
    public int OriginalYear { get; private set; }

    /// <summary>
    /// The month of the original period being adjusted.
    /// </summary>
    public int OriginalMonth { get; private set; }

    /// <summary>
    /// The adjustment amount (positive for credit, negative for debit).
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Reason for the adjustment.
    /// </summary>
    public string Reason { get; private set; } = default!;

    /// <summary>
    /// Related order ID if this adjustment is for a specific order.
    /// </summary>
    public Guid? RelatedOrderId { get; private set; }

    /// <summary>
    /// Related order number for display.
    /// </summary>
    public string? RelatedOrderNumber { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private SettlementAdjustment()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new settlement adjustment.
    /// </summary>
    public SettlementAdjustment(
        Guid settlementId,
        int originalYear,
        int originalMonth,
        decimal amount,
        string reason,
        Guid? relatedOrderId = null,
        string? relatedOrderNumber = null)
    {
        if (settlementId == Guid.Empty)
        {
            throw new ArgumentException("Settlement ID is required.", nameof(settlementId));
        }

        if (originalYear < 2020 || originalYear > 2100)
        {
            throw new ArgumentException("Original year must be between 2020 and 2100.", nameof(originalYear));
        }

        if (originalMonth < 1 || originalMonth > 12)
        {
            throw new ArgumentException("Original month must be between 1 and 12.", nameof(originalMonth));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        Id = Guid.NewGuid();
        SettlementId = settlementId;
        OriginalYear = originalYear;
        OriginalMonth = originalMonth;
        Amount = amount;
        Reason = reason.Trim();
        RelatedOrderId = relatedOrderId;
        RelatedOrderNumber = relatedOrderNumber;
        CreatedAt = DateTime.UtcNow;
    }
}
