using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get an invoice by ID.
/// </summary>
public sealed record GetCommissionInvoiceByIdQuery(Guid InvoiceId);

/// <summary>
/// Query to get invoice details with lines.
/// </summary>
public sealed record GetCommissionInvoiceDetailsQuery(Guid InvoiceId);

/// <summary>
/// Query to get invoices for a store with pagination.
/// </summary>
public sealed record GetCommissionInvoicesByStoreIdQuery(
    Guid StoreId,
    int Skip = 0,
    int Take = 20);

/// <summary>
/// Query to get invoices with filtering and pagination.
/// </summary>
public sealed record GetCommissionInvoicesQuery(
    Guid? StoreId = null,
    int? Year = null,
    int? Month = null,
    CommissionInvoiceStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get invoice PDF data.
/// </summary>
public sealed record GetInvoicePdfDataQuery(Guid InvoiceId);

/// <summary>
/// Query to get a credit note by ID.
/// </summary>
public sealed record GetCreditNoteByIdQuery(Guid CreditNoteId);

/// <summary>
/// Query to get credit notes for a store with pagination.
/// </summary>
public sealed record GetCreditNotesByStoreIdQuery(
    Guid StoreId,
    int Skip = 0,
    int Take = 20);

/// <summary>
/// Query to get credit notes for an invoice.
/// </summary>
public sealed record GetCreditNotesByInvoiceIdQuery(Guid InvoiceId);

/// <summary>
/// Query to get credit note PDF data.
/// </summary>
public sealed record GetCreditNotePdfDataQuery(Guid CreditNoteId);
