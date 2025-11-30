using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the credit note repository.
/// </summary>
public sealed class CreditNoteRepository : ICreditNoteRepository
{
    private readonly AppDbContext _context;

    public CreditNoteRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var creditNote = await _context.CreditNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);

        if (creditNote is not null)
        {
            var lines = await _context.CreditNoteLines
                .AsNoTracking()
                .Where(l => l.CreditNoteId == id)
                .ToListAsync(cancellationToken);

            creditNote.LoadLines(lines);
        }

        return creditNote;
    }

    /// <inheritdoc />
    public async Task<CreditNote?> GetByCreditNoteNumberAsync(string creditNoteNumber, CancellationToken cancellationToken = default)
    {
        var creditNote = await _context.CreditNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(cn => cn.CreditNoteNumber == creditNoteNumber, cancellationToken);

        if (creditNote is not null)
        {
            var lines = await _context.CreditNoteLines
                .AsNoTracking()
                .Where(l => l.CreditNoteId == creditNote.Id)
                .ToListAsync(cancellationToken);

            creditNote.LoadLines(lines);
        }

        return creditNote;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CreditNote>> GetByOriginalInvoiceIdAsync(Guid originalInvoiceId, CancellationToken cancellationToken = default)
    {
        var creditNotes = await _context.CreditNotes
            .AsNoTracking()
            .Where(cn => cn.OriginalInvoiceId == originalInvoiceId)
            .OrderByDescending(cn => cn.CreatedAt)
            .ToListAsync(cancellationToken);

        // Load lines for each credit note
        foreach (var creditNote in creditNotes)
        {
            var lines = await _context.CreditNoteLines
                .AsNoTracking()
                .Where(l => l.CreditNoteId == creditNote.Id)
                .ToListAsync(cancellationToken);

            creditNote.LoadLines(lines);
        }

        return creditNotes;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CreditNote> CreditNotes, int TotalCount)> GetByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CreditNotes
            .AsNoTracking()
            .Where(cn => cn.StoreId == storeId);

        var totalCount = await query.CountAsync(cancellationToken);

        var creditNotes = await query
            .OrderByDescending(cn => cn.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load lines for each credit note
        foreach (var creditNote in creditNotes)
        {
            var lines = await _context.CreditNoteLines
                .AsNoTracking()
                .Where(l => l.CreditNoteId == creditNote.Id)
                .ToListAsync(cancellationToken);

            creditNote.LoadLines(lines);
        }

        return (creditNotes, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CreditNote> CreditNotes, int TotalCount)> GetFilteredAsync(
        Guid? storeId,
        int? year,
        CreditNoteType? type,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CreditNotes.AsNoTracking();

        if (storeId.HasValue)
        {
            query = query.Where(cn => cn.StoreId == storeId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(cn => cn.IssueDate.Year == year.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(cn => cn.Type == type.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var creditNotes = await query
            .OrderByDescending(cn => cn.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load lines for each credit note
        foreach (var creditNote in creditNotes)
        {
            var lines = await _context.CreditNoteLines
                .AsNoTracking()
                .Where(l => l.CreditNoteId == creditNote.Id)
                .ToListAsync(cancellationToken);

            creditNote.LoadLines(lines);
        }

        return (creditNotes, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        // Check if there are any credit notes for this year
        var count = await _context.CreditNotes
            .Where(cn => cn.IssueDate.Year == year)
            .CountAsync(cancellationToken);

        if (count == 0)
        {
            return 1;
        }

        // For InMemory database, we need to load and process in memory
        // In production with SQL, this could be optimized with raw SQL
        var numbers = await _context.CreditNotes
            .Where(cn => cn.IssueDate.Year == year)
            .Select(cn => cn.CreditNoteNumber)
            .ToListAsync(cancellationToken);

        // Extract sequence numbers from credit note numbers like "CN-2024-00001"
        var maxSequence = numbers
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
    public async Task AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default)
    {
        await _context.CreditNotes.AddAsync(creditNote, cancellationToken);
        
        foreach (var line in creditNote.Lines)
        {
            await _context.CreditNoteLines.AddAsync(line, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(CreditNote creditNote, CancellationToken cancellationToken = default)
    {
        _context.CreditNotes.Update(creditNote);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
