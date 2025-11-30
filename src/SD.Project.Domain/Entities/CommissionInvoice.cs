namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a commission invoice.
/// </summary>
public enum CommissionInvoiceStatus
{
    /// <summary>Invoice is being generated and can still be modified.</summary>
    Draft,
    /// <summary>Invoice has been finalized and is ready for payment.</summary>
    Issued,
    /// <summary>Invoice has been paid.</summary>
    Paid,
    /// <summary>Invoice has been cancelled.</summary>
    Cancelled,
    /// <summary>Invoice has been corrected by a credit note.</summary>
    Corrected
}

/// <summary>
/// Represents a commission invoice for a seller.
/// Generated monthly based on settlement data.
/// Invoices follow legal requirements with unique sequential numbering.
/// </summary>
public class CommissionInvoice
{
    private readonly List<CommissionInvoiceLine> _lines = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The store (seller) this invoice is for.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The seller's user ID.
    /// </summary>
    public Guid SellerId { get; private set; }

    /// <summary>
    /// The settlement this invoice is based on.
    /// </summary>
    public Guid SettlementId { get; private set; }

    /// <summary>
    /// Unique sequential invoice number.
    /// Format: INV-{YYYY}-{NNNNN} where NNNNN is sequential within the year.
    /// </summary>
    public string InvoiceNumber { get; private set; } = default!;

    /// <summary>
    /// The year of this invoice period.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// The month of this invoice period (1-12).
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Current status of the invoice.
    /// </summary>
    public CommissionInvoiceStatus Status { get; private set; }

    /// <summary>
    /// Currency code for all amounts.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Net amount before tax.
    /// </summary>
    public decimal NetAmount { get; private set; }

    /// <summary>
    /// Tax rate as a percentage (e.g., 23 for 23% VAT).
    /// </summary>
    public decimal TaxRate { get; private set; }

    /// <summary>
    /// Tax amount calculated from net amount and tax rate.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Gross amount including tax.
    /// </summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>
    /// The date the invoice was issued.
    /// </summary>
    public DateTime IssueDate { get; private set; }

    /// <summary>
    /// The due date for payment.
    /// </summary>
    public DateTime DueDate { get; private set; }

    /// <summary>
    /// Start date of the billing period.
    /// </summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>
    /// End date of the billing period.
    /// </summary>
    public DateTime PeriodEnd { get; private set; }

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
    /// Optional notes or description.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Reference to the correcting credit note if this invoice was corrected.
    /// </summary>
    public Guid? CorrectedByNoteId { get; private set; }

    /// <summary>
    /// Invoice lines detailing the commission charges.
    /// </summary>
    public IReadOnlyCollection<CommissionInvoiceLine> Lines => _lines.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? IssuedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private CommissionInvoice()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new commission invoice.
    /// </summary>
    public CommissionInvoice(
        Guid storeId,
        Guid sellerId,
        Guid settlementId,
        string invoiceNumber,
        int year,
        int month,
        string currency,
        decimal taxRate,
        DateTime issueDate,
        DateTime dueDate,
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

        if (settlementId == Guid.Empty)
        {
            throw new ArgumentException("Settlement ID is required.", nameof(settlementId));
        }

        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));
        }

        if (year < 2020 || year > 2100)
        {
            throw new ArgumentException("Year must be between 2020 and 2100.", nameof(year));
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12.", nameof(month));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (taxRate < 0 || taxRate > 100)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(taxRate));
        }

        if (dueDate < issueDate)
        {
            throw new ArgumentException("Due date must be on or after issue date.", nameof(dueDate));
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
        SettlementId = settlementId;
        InvoiceNumber = invoiceNumber;
        Year = year;
        Month = month;
        Status = CommissionInvoiceStatus.Draft;
        Currency = currency.ToUpperInvariant();
        TaxRate = taxRate;
        IssueDate = issueDate;
        DueDate = dueDate;

        // Calculate period dates
        PeriodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        PeriodEnd = PeriodStart.AddMonths(1).AddTicks(-1);

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
    /// Adds a line item to the invoice.
    /// </summary>
    public CommissionInvoiceLine AddLine(
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        if (Status != CommissionInvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add lines to invoice in status {Status}.");
        }

        var line = new CommissionInvoiceLine(
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
    /// Issues the invoice, making it official.
    /// </summary>
    public void Issue()
    {
        if (Status != CommissionInvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot issue invoice in status {Status}.");
        }

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("Cannot issue invoice with no lines.");
        }

        Status = CommissionInvoiceStatus.Issued;
        IssuedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the invoice as paid.
    /// </summary>
    public void MarkPaid()
    {
        if (Status != CommissionInvoiceStatus.Issued)
        {
            throw new InvalidOperationException($"Cannot mark invoice as paid in status {Status}.");
        }

        Status = CommissionInvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the invoice.
    /// </summary>
    public void Cancel()
    {
        if (Status == CommissionInvoiceStatus.Paid || Status == CommissionInvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel invoice in status {Status}.");
        }

        Status = CommissionInvoiceStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this invoice as corrected by a credit note.
    /// </summary>
    public void MarkCorrected(Guid creditNoteId)
    {
        if (creditNoteId == Guid.Empty)
        {
            throw new ArgumentException("Credit note ID is required.", nameof(creditNoteId));
        }

        if (Status != CommissionInvoiceStatus.Issued && Status != CommissionInvoiceStatus.Paid)
        {
            throw new InvalidOperationException($"Cannot correct invoice in status {Status}.");
        }

        Status = CommissionInvoiceStatus.Corrected;
        CorrectedByNoteId = creditNoteId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the notes for this invoice.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        if (Status != CommissionInvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot update notes on invoice in status {Status}.");
        }

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
    public void LoadLines(IEnumerable<CommissionInvoiceLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        RecalculateTotals();
    }
}
