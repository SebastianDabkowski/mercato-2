using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository for managing credit notes.
/// </summary>
public interface ICreditNoteRepository
{
    /// <summary>
    /// Gets a credit note by ID.
    /// </summary>
    Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a credit note by credit note number.
    /// </summary>
    Task<CreditNote?> GetByCreditNoteNumberAsync(string creditNoteNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credit notes for an original invoice.
    /// </summary>
    Task<IReadOnlyList<CreditNote>> GetByOriginalInvoiceIdAsync(Guid originalInvoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credit notes for a store with pagination.
    /// </summary>
    Task<(IReadOnlyList<CreditNote> CreditNotes, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credit notes filtered by various criteria with pagination.
    /// </summary>
    Task<(IReadOnlyList<CreditNote> CreditNotes, int TotalCount)> GetFilteredAsync(
        Guid? storeId,
        int? year,
        CreditNoteType? type,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next sequential credit note number for a year.
    /// </summary>
    Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new credit note.
    /// </summary>
    Task AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing credit note.
    /// </summary>
    Task UpdateAsync(CreditNote creditNote, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
