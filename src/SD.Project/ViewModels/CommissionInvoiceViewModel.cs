namespace SD.Project.ViewModels;

/// <summary>
/// View model for commission invoice list item.
/// </summary>
public sealed record CommissionInvoiceListItemViewModel(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string InvoiceNumber,
    int Year,
    int Month,
    string Status,
    string Currency,
    decimal GrossAmount,
    DateTime IssueDate,
    DateTime DueDate,
    DateTime CreatedAt,
    bool HasCreditNote)
{
    /// <summary>
    /// Gets the period display string (e.g., "December 2024").
    /// </summary>
    public string PeriodDisplay => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Issued" => "bg-info",
        "Paid" => "bg-success",
        "Cancelled" => "bg-danger",
        "Corrected" => "bg-warning",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Checks if the invoice is overdue.
    /// </summary>
    public bool IsOverdue => Status == "Issued" && DueDate < DateTime.UtcNow;
}

/// <summary>
/// View model for detailed commission invoice view with lines.
/// </summary>
public sealed record CommissionInvoiceDetailsViewModel(
    Guid Id,
    Guid StoreId,
    Guid SellerId,
    Guid SettlementId,
    string InvoiceNumber,
    int Year,
    int Month,
    string Status,
    string Currency,
    decimal NetAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal GrossAmount,
    DateTime IssueDate,
    DateTime DueDate,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string SellerName,
    string? SellerTaxId,
    string SellerAddress,
    string SellerCity,
    string SellerPostalCode,
    string SellerCountry,
    string IssuerName,
    string? IssuerTaxId,
    string IssuerAddress,
    string IssuerCity,
    string IssuerPostalCode,
    string IssuerCountry,
    IReadOnlyList<CommissionInvoiceLineViewModel> Lines,
    IReadOnlyList<CreditNoteListItemViewModel>? CreditNotes,
    string? Notes,
    Guid? CorrectedByNoteId,
    DateTime CreatedAt,
    DateTime? IssuedAt,
    DateTime? PaidAt,
    DateTime? CancelledAt)
{
    /// <summary>
    /// Gets the period display string (e.g., "December 2024").
    /// </summary>
    public string PeriodDisplay => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Issued" => "bg-info",
        "Paid" => "bg-success",
        "Cancelled" => "bg-danger",
        "Corrected" => "bg-warning",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Checks if the invoice is overdue.
    /// </summary>
    public bool IsOverdue => Status == "Issued" && DueDate < DateTime.UtcNow;

    /// <summary>
    /// Checks if the invoice can be downloaded as PDF.
    /// </summary>
    public bool CanDownloadPdf => Status != "Draft";

    /// <summary>
    /// Gets the formatted issuer address.
    /// </summary>
    public string FormattedIssuerAddress => $"{IssuerAddress}, {IssuerPostalCode} {IssuerCity}, {IssuerCountry}";

    /// <summary>
    /// Gets the formatted seller address.
    /// </summary>
    public string FormattedSellerAddress => $"{SellerAddress}, {SellerPostalCode} {SellerCity}, {SellerCountry}";
}

/// <summary>
/// View model for commission invoice line.
/// </summary>
public sealed record CommissionInvoiceLineViewModel(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount);

/// <summary>
/// View model for credit note list item.
/// </summary>
public sealed record CreditNoteListItemViewModel(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string CreditNoteNumber,
    string OriginalInvoiceNumber,
    string Type,
    string Currency,
    decimal GrossAmount,
    DateTime IssueDate,
    string Reason,
    DateTime CreatedAt)
{
    /// <summary>
    /// Gets the type badge CSS class.
    /// </summary>
    public string TypeBadgeClass => Type switch
    {
        "Full" => "bg-danger",
        "Partial" => "bg-warning",
        _ => "bg-secondary"
    };
}

/// <summary>
/// View model for detailed credit note view.
/// </summary>
public sealed record CreditNoteDetailsViewModel(
    Guid Id,
    Guid StoreId,
    Guid SellerId,
    Guid OriginalInvoiceId,
    string OriginalInvoiceNumber,
    string CreditNoteNumber,
    string Type,
    string Currency,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount,
    DateTime IssueDate,
    string SellerName,
    string? SellerTaxId,
    string SellerAddress,
    string SellerCity,
    string SellerPostalCode,
    string SellerCountry,
    string IssuerName,
    string? IssuerTaxId,
    string IssuerAddress,
    string IssuerCity,
    string IssuerPostalCode,
    string IssuerCountry,
    string Reason,
    string? Notes,
    IReadOnlyList<CreditNoteLineViewModel> Lines,
    DateTime CreatedAt)
{
    /// <summary>
    /// Gets the type badge CSS class.
    /// </summary>
    public string TypeBadgeClass => Type switch
    {
        "Full" => "bg-danger",
        "Partial" => "bg-warning",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the formatted issuer address.
    /// </summary>
    public string FormattedIssuerAddress => $"{IssuerAddress}, {IssuerPostalCode} {IssuerCity}, {IssuerCountry}";

    /// <summary>
    /// Gets the formatted seller address.
    /// </summary>
    public string FormattedSellerAddress => $"{SellerAddress}, {SellerPostalCode} {SellerCity}, {SellerCountry}";
}

/// <summary>
/// View model for credit note line.
/// </summary>
public sealed record CreditNoteLineViewModel(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount);
