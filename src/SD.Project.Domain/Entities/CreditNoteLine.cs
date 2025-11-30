namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a line item on a credit note.
/// </summary>
public class CreditNoteLine
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The credit note this line belongs to.
    /// </summary>
    public Guid CreditNoteId { get; private set; }

    /// <summary>
    /// Description of the correction.
    /// </summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Quantity (typically negative for credits).
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Tax rate as a percentage for this line.
    /// </summary>
    public decimal TaxRate { get; private set; }

    /// <summary>
    /// Net amount (Quantity * UnitPrice) - typically negative.
    /// </summary>
    public decimal NetAmount { get; private set; }

    /// <summary>
    /// Tax amount for this line.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Gross amount including tax.
    /// </summary>
    public decimal GrossAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private CreditNoteLine()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new credit note line.
    /// </summary>
    public CreditNoteLine(
        Guid creditNoteId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        if (creditNoteId == Guid.Empty)
        {
            throw new ArgumentException("Credit note ID is required.", nameof(creditNoteId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (taxRate < 0 || taxRate > 100)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(taxRate));
        }

        Id = Guid.NewGuid();
        CreditNoteId = creditNoteId;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;

        // Calculate amounts (typically negative for credits)
        NetAmount = Math.Round(Quantity * UnitPrice, 2);
        TaxAmount = Math.Round(NetAmount * TaxRate / 100, 2);
        GrossAmount = NetAmount + TaxAmount;

        CreatedAt = DateTime.UtcNow;
    }
}
