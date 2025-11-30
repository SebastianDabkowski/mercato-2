using SD.Project.Application.DTOs;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Interface for generating PDF documents.
/// </summary>
public interface IPdfGeneratorService
{
    /// <summary>
    /// Generates a PDF for a commission invoice.
    /// </summary>
    /// <param name="invoiceData">The invoice data to render.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PDF file as a byte array.</returns>
    Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfDataDto invoiceData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF for a credit note.
    /// </summary>
    /// <param name="creditNoteData">The credit note data to render.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PDF file as a byte array.</returns>
    Task<byte[]> GenerateCreditNotePdfAsync(CreditNotePdfDataDto creditNoteData, CancellationToken cancellationToken = default);
}
