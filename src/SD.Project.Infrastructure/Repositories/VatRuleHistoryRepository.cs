using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the VAT rule history repository.
/// </summary>
public class VatRuleHistoryRepository : IVatRuleHistoryRepository
{
    private readonly AppDbContext _context;

    public VatRuleHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<VatRuleHistory>> GetByVatRuleIdAsync(Guid vatRuleId, CancellationToken cancellationToken = default)
    {
        return await _context.VatRuleHistories
            .Where(h => h.VatRuleId == vatRuleId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatRuleHistory>> GetAllAsync(string? countryCode = null, CancellationToken cancellationToken = default)
    {
        var query = _context.VatRuleHistories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var normalizedCode = countryCode.ToUpperInvariant();
            query = query.Where(h => h.CountryCode == normalizedCode);
        }

        return await query
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatRuleHistory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.VatRuleHistories
            .Where(h => h.ChangedByUserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatRuleHistory>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.VatRuleHistories
            .Where(h => h.CreatedAt >= from && h.CreatedAt <= to)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(VatRuleHistory history, CancellationToken cancellationToken = default)
    {
        await _context.VatRuleHistories.AddAsync(history, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
