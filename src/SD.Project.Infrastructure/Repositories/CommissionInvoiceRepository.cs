using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the commission invoice repository.
/// </summary>
public sealed class CommissionInvoiceRepository : ICommissionInvoiceRepository
{
    private readonly AppDbContext _context;

    public CommissionInvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.CommissionInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is not null)
        {
            var lines = await _context.CommissionInvoiceLines
                .AsNoTracking()
                .Where(l => l.InvoiceId == id)
                .ToListAsync(cancellationToken);

            invoice.LoadLines(lines);
        }

        return invoice;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.CommissionInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

        if (invoice is not null)
        {
            var lines = await _context.CommissionInvoiceLines
                .AsNoTracking()
                .Where(l => l.InvoiceId == invoice.Id)
                .ToListAsync(cancellationToken);

            invoice.LoadLines(lines);
        }

        return invoice;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetBySettlementIdAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.CommissionInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SettlementId == settlementId, cancellationToken);

        if (invoice is not null)
        {
            var lines = await _context.CommissionInvoiceLines
                .AsNoTracking()
                .Where(l => l.InvoiceId == invoice.Id)
                .ToListAsync(cancellationToken);

            invoice.LoadLines(lines);
        }

        return invoice;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CommissionInvoice> Invoices, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommissionInvoices
            .AsNoTracking()
            .Where(i => i.StoreId == storeId);

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ThenByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load lines for each invoice
        foreach (var invoice in invoices)
        {
            var lines = await _context.CommissionInvoiceLines
                .AsNoTracking()
                .Where(l => l.InvoiceId == invoice.Id)
                .ToListAsync(cancellationToken);

            invoice.LoadLines(lines);
        }

        return (invoices, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CommissionInvoice> Invoices, int TotalCount)> GetFilteredAsync(
        Guid? storeId,
        int? year,
        int? month,
        CommissionInvoiceStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommissionInvoices.AsNoTracking();

        if (storeId.HasValue)
        {
            query = query.Where(i => i.StoreId == storeId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(i => i.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(i => i.Month == month.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ThenByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load lines for each invoice
        foreach (var invoice in invoices)
        {
            var lines = await _context.CommissionInvoiceLines
                .AsNoTracking()
                .Where(l => l.InvoiceId == invoice.Id)
                .ToListAsync(cancellationToken);

            invoice.LoadLines(lines);
        }

        return (invoices, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        // Count existing invoices for this year and add 1
        // This is more efficient than loading all invoice numbers into memory
        var count = await _context.CommissionInvoices
            .Where(i => i.Year == year)
            .CountAsync(cancellationToken);

        // If there are existing invoices, we need to find the max sequence number
        // since sequence numbers might have gaps due to deletions
        if (count == 0)
        {
            return 1;
        }

        // For InMemory database, we need to load and process in memory
        // In production with SQL, this could be optimized with raw SQL
        var maxNumber = await _context.CommissionInvoices
            .Where(i => i.Year == year)
            .Select(i => i.InvoiceNumber)
            .ToListAsync(cancellationToken);

        // Extract sequence numbers from invoice numbers like "INV-2024-00001"
        var maxSequence = maxNumber
            .Select(n =>
            {
                var parts = n.Split('-');
                return parts.Length == 3 && int.TryParse(parts[2], out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return maxSequence + 1;
    }

    /// <inheritdoc />
    public async Task AddAsync(CommissionInvoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.CommissionInvoices.AddAsync(invoice, cancellationToken);
        
        foreach (var line in invoice.Lines)
        {
            await _context.CommissionInvoiceLines.AddAsync(line, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(CommissionInvoice invoice, CancellationToken cancellationToken = default)
    {
        _context.CommissionInvoices.Update(invoice);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
