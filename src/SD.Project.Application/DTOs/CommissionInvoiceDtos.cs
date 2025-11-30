namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for commission invoice information.
/// </summary>
public sealed record CommissionInvoiceDto(
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
    string? Notes,
    Guid? CorrectedByNoteId,
    DateTime CreatedAt,
    DateTime? IssuedAt,
    DateTime? PaidAt,
    DateTime? CancelledAt);

/// <summary>
/// DTO for commission invoice list item.
/// </summary>
public sealed record CommissionInvoiceListItemDto(
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
    bool HasCreditNote);

/// <summary>
/// DTO for detailed commission invoice view with lines.
/// </summary>
public sealed record CommissionInvoiceDetailsDto(
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
    IReadOnlyList<CommissionInvoiceLineDto> Lines,
    IReadOnlyList<CreditNoteListItemDto>? CreditNotes,
    string? Notes,
    Guid? CorrectedByNoteId,
    DateTime CreatedAt,
    DateTime? IssuedAt,
    DateTime? PaidAt,
    DateTime? CancelledAt);

/// <summary>
/// DTO for commission invoice line.
/// </summary>
public sealed record CommissionInvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount);

/// <summary>
/// DTO for credit note information.
/// </summary>
public sealed record CreditNoteDto(
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
    IReadOnlyList<CreditNoteLineDto> Lines,
    DateTime CreatedAt);

/// <summary>
/// DTO for credit note list item.
/// </summary>
public sealed record CreditNoteListItemDto(
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
    DateTime CreatedAt);

/// <summary>
/// DTO for credit note line.
/// </summary>
public sealed record CreditNoteLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount);

/// <summary>
/// Result DTO for generate invoice operations.
/// </summary>
public sealed record GenerateInvoiceResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? InvoiceId,
    string? InvoiceNumber,
    decimal? GrossAmount)
{
    public static GenerateInvoiceResultDto Succeeded(
        Guid invoiceId,
        string invoiceNumber,
        decimal grossAmount) =>
        new(true, null, invoiceId, invoiceNumber, grossAmount);

    public static GenerateInvoiceResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null);

    public static GenerateInvoiceResultDto AlreadyExists(Guid invoiceId, string invoiceNumber) =>
        new(false, "Invoice already exists for this settlement.", invoiceId, invoiceNumber, null);
}

/// <summary>
/// Result DTO for issue invoice operations.
/// </summary>
public sealed record IssueInvoiceResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? InvoiceId)
{
    public static IssueInvoiceResultDto Succeeded(Guid invoiceId) =>
        new(true, null, invoiceId);

    public static IssueInvoiceResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null);
}

/// <summary>
/// Result DTO for create credit note operations.
/// </summary>
public sealed record CreateCreditNoteResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? CreditNoteId,
    string? CreditNoteNumber,
    decimal? GrossAmount)
{
    public static CreateCreditNoteResultDto Succeeded(
        Guid creditNoteId,
        string creditNoteNumber,
        decimal grossAmount) =>
        new(true, null, creditNoteId, creditNoteNumber, grossAmount);

    public static CreateCreditNoteResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null);
}

/// <summary>
/// DTO for invoice PDF generation data.
/// </summary>
public sealed record InvoicePdfDataDto(
    string InvoiceNumber,
    DateTime IssueDate,
    DateTime DueDate,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Currency,
    decimal NetAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal GrossAmount,
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
    IReadOnlyList<CommissionInvoiceLineDto> Lines,
    string? Notes);

/// <summary>
/// DTO for credit note PDF generation data.
/// </summary>
public sealed record CreditNotePdfDataDto(
    string CreditNoteNumber,
    string OriginalInvoiceNumber,
    DateTime IssueDate,
    string Type,
    string Currency,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount,
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
    IReadOnlyList<CreditNoteLineDto> Lines,
    string Reason,
    string? Notes);
