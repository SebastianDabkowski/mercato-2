using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to generate a commission invoice from a settlement.
/// </summary>
public sealed record GenerateCommissionInvoiceCommand(
    Guid SettlementId,
    decimal TaxRate = 23m,
    int PaymentDueDays = 14);

/// <summary>
/// Command to generate invoices for all approved settlements in a period.
/// </summary>
public sealed record GenerateAllCommissionInvoicesCommand(
    int Year,
    int Month,
    decimal TaxRate = 23m,
    int PaymentDueDays = 14);

/// <summary>
/// Command to issue a draft invoice.
/// </summary>
public sealed record IssueCommissionInvoiceCommand(Guid InvoiceId);

/// <summary>
/// Command to mark an invoice as paid.
/// </summary>
public sealed record MarkInvoicePaidCommand(Guid InvoiceId);

/// <summary>
/// Command to cancel an invoice.
/// </summary>
public sealed record CancelInvoiceCommand(Guid InvoiceId);

/// <summary>
/// Command to update invoice notes.
/// </summary>
public sealed record UpdateInvoiceNotesCommand(
    Guid InvoiceId,
    string? Notes);

/// <summary>
/// Command to create a credit note for an invoice.
/// </summary>
public sealed record CreateCreditNoteCommand(
    Guid OriginalInvoiceId,
    CreditNoteType Type,
    string Reason,
    IReadOnlyList<CreditNoteLineInput>? Lines = null);

/// <summary>
/// Input for a credit note line.
/// </summary>
public sealed record CreditNoteLineInput(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate);

/// <summary>
/// Command to update credit note notes.
/// </summary>
public sealed record UpdateCreditNoteNotesCommand(
    Guid CreditNoteId,
    string? Notes);
