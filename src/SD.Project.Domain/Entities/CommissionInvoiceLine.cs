namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a line item on a commission invoice.
/// </summary>
public class CommissionInvoiceLine
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The invoice this line belongs to.
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Description of the service/charge.
    /// </summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Quantity (typically 1 for commission services).
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Unit price before tax.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Tax rate as a percentage for this line.
    /// </summary>
    public decimal TaxRate { get; private set; }

    /// <summary>
    /// Net amount (Quantity * UnitPrice).
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

    private CommissionInvoiceLine()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new invoice line.
    /// </summary>
    public CommissionInvoiceLine(
        Guid invoiceId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        if (invoiceId == Guid.Empty)
        {
            throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (taxRate < 0 || taxRate > 100)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(taxRate));
        }

        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;

        // Calculate amounts
        NetAmount = Math.Round(Quantity * UnitPrice, 2);
        TaxAmount = Math.Round(NetAmount * TaxRate / 100, 2);
        GrossAmount = NetAmount + TaxAmount;

        CreatedAt = DateTime.UtcNow;
    }
}
