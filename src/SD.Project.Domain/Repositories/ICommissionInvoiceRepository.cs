using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository for managing commission invoices.
/// </summary>
public interface ICommissionInvoiceRepository
{
    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    Task<CommissionInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an invoice by invoice number.
    /// </summary>
    Task<CommissionInvoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the invoice for a specific settlement.
    /// </summary>
    Task<CommissionInvoice?> GetBySettlementIdAsync(Guid settlementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices for a store with pagination.
    /// </summary>
    Task<(IReadOnlyList<CommissionInvoice> Invoices, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices filtered by various criteria with pagination.
    /// </summary>
    Task<(IReadOnlyList<CommissionInvoice> Invoices, int TotalCount)> GetFilteredAsync(
        Guid? storeId,
        int? year,
        int? month,
        CommissionInvoiceStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next sequential invoice number for a year.
    /// </summary>
    Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new invoice.
    /// </summary>
    Task AddAsync(CommissionInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing invoice.
    /// </summary>
    Task UpdateAsync(CommissionInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
