namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the type of credit note.
/// </summary>
public enum CreditNoteType
{
    /// <summary>Full credit (cancels entire invoice).</summary>
    Full,
    /// <summary>Partial credit (corrects specific amounts).</summary>
    Partial
}

/// <summary>
/// Represents a credit note that corrects a commission invoice.
/// Credit notes follow legal requirements with unique sequential numbering.
/// </summary>
public class CreditNote
{
    private readonly List<CreditNoteLine> _lines = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The store (seller) this credit note is for.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The seller's user ID.
    /// </summary>
    public Guid SellerId { get; private set; }

    /// <summary>
    /// The original invoice this credit note corrects.
    /// </summary>
    public Guid OriginalInvoiceId { get; private set; }

    /// <summary>
    /// The original invoice number for reference.
    /// </summary>
    public string OriginalInvoiceNumber { get; private set; } = default!;

    /// <summary>
    /// Unique sequential credit note number.
    /// Format: CN-{YYYY}-{NNNNN} where NNNNN is sequential within the year.
    /// </summary>
    public string CreditNoteNumber { get; private set; } = default!;

    /// <summary>
    /// Type of credit note (full or partial).
    /// </summary>
    public CreditNoteType Type { get; private set; }

    /// <summary>
    /// Currency code for all amounts.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Net amount credited (negative for credits).
    /// </summary>
    public decimal NetAmount { get; private set; }

    /// <summary>
    /// Tax amount credited.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Gross amount credited.
    /// </summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>
    /// The date the credit note was issued.
    /// </summary>
    public DateTime IssueDate { get; private set; }

    // Seller/Store billing information (denormalized for historical record)
    public string SellerName { get; private set; } = default!;
    public string? SellerTaxId { get; private set; }
    public string SellerAddress { get; private set; } = default!;
    public string SellerCity { get; private set; } = default!;
    public string SellerPostalCode { get; private set; } = default!;
    public string SellerCountry { get; private set; } = default!;

    // Platform billing information (issuer)
    public string IssuerName { get; private set; } = default!;
    public string? IssuerTaxId { get; private set; }
    public string IssuerAddress { get; private set; } = default!;
    public string IssuerCity { get; private set; } = default!;
    public string IssuerPostalCode { get; private set; } = default!;
    public string IssuerCountry { get; private set; } = default!;

    /// <summary>
    /// Reason for issuing the credit note.
    /// </summary>
    public string Reason { get; private set; } = default!;

    /// <summary>
    /// Optional additional notes.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Credit note lines detailing the corrections.
    /// </summary>
    public IReadOnlyCollection<CreditNoteLine> Lines => _lines.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CreditNote()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new credit note.
    /// </summary>
    public CreditNote(
        Guid storeId,
        Guid sellerId,
        Guid originalInvoiceId,
        string originalInvoiceNumber,
        string creditNoteNumber,
        CreditNoteType type,
        string currency,
        DateTime issueDate,
        string reason,
        string sellerName,
        string? sellerTaxId,
        string sellerAddress,
        string sellerCity,
        string sellerPostalCode,
        string sellerCountry,
        string issuerName,
        string? issuerTaxId,
        string issuerAddress,
        string issuerCity,
        string issuerPostalCode,
        string issuerCountry)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller ID is required.", nameof(sellerId));
        }

        if (originalInvoiceId == Guid.Empty)
        {
            throw new ArgumentException("Original invoice ID is required.", nameof(originalInvoiceId));
        }

        if (string.IsNullOrWhiteSpace(originalInvoiceNumber))
        {
            throw new ArgumentException("Original invoice number is required.", nameof(originalInvoiceNumber));
        }

        if (string.IsNullOrWhiteSpace(creditNoteNumber))
        {
            throw new ArgumentException("Credit note number is required.", nameof(creditNoteNumber));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(sellerName))
        {
            throw new ArgumentException("Seller name is required.", nameof(sellerName));
        }

        if (string.IsNullOrWhiteSpace(issuerName))
        {
            throw new ArgumentException("Issuer name is required.", nameof(issuerName));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        SellerId = sellerId;
        OriginalInvoiceId = originalInvoiceId;
        OriginalInvoiceNumber = originalInvoiceNumber;
        CreditNoteNumber = creditNoteNumber;
        Type = type;
        Currency = currency.ToUpperInvariant();
        IssueDate = issueDate;
        Reason = reason.Trim();

        // Seller information
        SellerName = sellerName.Trim();
        SellerTaxId = sellerTaxId?.Trim();
        SellerAddress = sellerAddress?.Trim() ?? string.Empty;
        SellerCity = sellerCity?.Trim() ?? string.Empty;
        SellerPostalCode = sellerPostalCode?.Trim() ?? string.Empty;
        SellerCountry = sellerCountry?.Trim() ?? string.Empty;

        // Issuer information
        IssuerName = issuerName.Trim();
        IssuerTaxId = issuerTaxId?.Trim();
        IssuerAddress = issuerAddress?.Trim() ?? string.Empty;
        IssuerCity = issuerCity?.Trim() ?? string.Empty;
        IssuerPostalCode = issuerPostalCode?.Trim() ?? string.Empty;
        IssuerCountry = issuerCountry?.Trim() ?? string.Empty;

        NetAmount = 0m;
        TaxAmount = 0m;
        GrossAmount = 0m;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a line item to the credit note.
    /// </summary>
    public CreditNoteLine AddLine(
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        var line = new CreditNoteLine(
            Id,
            description,
            quantity,
            unitPrice,
            taxRate);

        _lines.Add(line);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;

        return line;
    }

    /// <summary>
    /// Updates the notes for this credit note.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        NetAmount = _lines.Sum(l => l.NetAmount);
        TaxAmount = _lines.Sum(l => l.TaxAmount);
        GrossAmount = _lines.Sum(l => l.GrossAmount);
    }

    /// <summary>
    /// Loads lines from persistence.
    /// </summary>
    public void LoadLines(IEnumerable<CreditNoteLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        RecalculateTotals();
    }
}
