using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the VAT rule repository.
/// </summary>
public class VatRuleRepository : IVatRuleRepository
{
    private readonly AppDbContext _context;

    public VatRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VatRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.VatRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<VatRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VatRules
            .OrderBy(r => r.CountryCode)
            .ThenBy(r => r.CategoryId.HasValue ? 1 : 0)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatRule>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.VatRules
            .Where(r => r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now))
            .OrderBy(r => r.CountryCode)
            .ThenBy(r => r.CategoryId.HasValue ? 1 : 0)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatRule>> GetByCountryAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = countryCode.ToUpperInvariant();
        return await _context.VatRules
            .Where(r => r.CountryCode == normalizedCode)
            .OrderBy(r => r.CategoryId.HasValue ? 1 : 0)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<VatRule?> GetEffectiveRuleAsync(
        string countryCode,
        Guid? categoryId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var dateToCheck = effectiveDate ?? DateTime.UtcNow;
        var normalizedCode = countryCode.ToUpperInvariant();

        // Priority: Category-specific > Country-wide
        // 1. Check for category-specific rule
        if (categoryId.HasValue)
        {
            var categoryRule = await _context.VatRules
                .Where(r => r.CountryCode == normalizedCode
                            && r.CategoryId == categoryId
                            && r.IsActive
                            && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= dateToCheck)
                            && (!r.EffectiveTo.HasValue || r.EffectiveTo >= dateToCheck))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (categoryRule is not null)
            {
                return categoryRule;
            }
        }

        // 2. Check for country-wide rule (no category)
        var countryRule = await _context.VatRules
            .Where(r => r.CountryCode == normalizedCode
                        && r.CategoryId == null
                        && r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= dateToCheck)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= dateToCheck))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return countryRule;
    }

    public async Task<IReadOnlyList<VatRule>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.VatRules
            .Where(r => r.CategoryId == categoryId)
            .OrderBy(r => r.CountryCode)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatRule>> GetOverlappingRulesAsync(
        string countryCode,
        Guid? categoryId,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = countryCode.ToUpperInvariant();

        var query = _context.VatRules
            .Where(r => r.IsActive
                        && r.CountryCode == normalizedCode
                        && r.CategoryId == categoryId);

        // Exclude the rule being updated
        if (excludeRuleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRuleId.Value);
        }

        var existingRules = await query.ToListAsync(cancellationToken);

        // Check for date overlap in memory
        return existingRules
            .Where(r => DateRangesOverlap(r.EffectiveFrom, r.EffectiveTo, effectiveFrom, effectiveTo))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetDistinctCountryCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VatRules
            .Select(r => r.CountryCode)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(VatRule rule, CancellationToken cancellationToken = default)
    {
        await _context.VatRules.AddAsync(rule, cancellationToken);
    }

    public Task UpdateAsync(VatRule rule, CancellationToken cancellationToken = default)
    {
        _context.VatRules.Update(rule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(VatRule rule, CancellationToken cancellationToken = default)
    {
        _context.VatRules.Remove(rule);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Determines if two date ranges overlap.
    /// Null dates are treated as unbounded (negative/positive infinity).
    /// </summary>
    private static bool DateRangesOverlap(DateTime? from1, DateTime? to1, DateTime? from2, DateTime? to2)
    {
        var from1Bounded = from1 ?? DateTime.MinValue;
        var to1Bounded = to1 ?? DateTime.MaxValue;
        var from2Bounded = from2 ?? DateTime.MinValue;
        var to2Bounded = to2 ?? DateTime.MaxValue;

        return from1Bounded <= to2Bounded && from2Bounded <= to1Bounded;
    }
}
