using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the commission rule repository.
/// </summary>
public class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly AppDbContext _context;

    public CommissionRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CommissionRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<CommissionRule?> GetActiveGlobalRuleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CommissionRules
            .Where(r => r.RuleType == CommissionRuleType.Global
                        && r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CommissionRule?> GetActiveCategoryRuleAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CommissionRules
            .Where(r => r.RuleType == CommissionRuleType.Category
                        && r.CategoryId == categoryId
                        && r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CommissionRule?> GetActiveSellerRuleAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CommissionRules
            .Where(r => r.RuleType == CommissionRuleType.Seller
                        && r.StoreId == storeId
                        && r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal?> GetEffectiveRateAsync(
        Guid storeId,
        Guid? categoryId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var dateToCheck = effectiveDate ?? DateTime.UtcNow;

        // Priority: Seller > Category > Global
        // 1. Check for seller-specific rule
        var sellerRule = await _context.CommissionRules
            .Where(r => r.RuleType == CommissionRuleType.Seller
                        && r.StoreId == storeId
                        && r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= dateToCheck)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= dateToCheck))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerRule is not null)
        {
            return sellerRule.CommissionRate;
        }

        // 2. Check for category-specific rule
        if (categoryId.HasValue)
        {
            var categoryRule = await _context.CommissionRules
                .Where(r => r.RuleType == CommissionRuleType.Category
                            && r.CategoryId == categoryId
                            && r.IsActive
                            && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= dateToCheck)
                            && (!r.EffectiveTo.HasValue || r.EffectiveTo >= dateToCheck))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (categoryRule is not null)
            {
                return categoryRule.CommissionRate;
            }
        }

        // 3. Check for global rule
        var globalRule = await _context.CommissionRules
            .Where(r => r.RuleType == CommissionRuleType.Global
                        && r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= dateToCheck)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= dateToCheck))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return globalRule?.CommissionRate;
    }

    public async Task<IReadOnlyList<CommissionRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRules
            .OrderBy(r => r.RuleType)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CommissionRules
            .Where(r => r.IsActive
                        && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                        && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now))
            .OrderBy(r => r.RuleType)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRules
            .Where(r => r.CategoryId == categoryId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionRules
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CommissionRule rule, CancellationToken cancellationToken = default)
    {
        await _context.CommissionRules.AddAsync(rule, cancellationToken);
    }

    public Task UpdateAsync(CommissionRule rule, CancellationToken cancellationToken = default)
    {
        _context.CommissionRules.Update(rule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CommissionRule rule, CancellationToken cancellationToken = default)
    {
        _context.CommissionRules.Remove(rule);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetOverlappingRulesAsync(
        CommissionRuleType ruleType,
        Guid? categoryId,
        Guid? storeId,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommissionRules
            .Where(r => r.IsActive && r.RuleType == ruleType);

        // Filter by scope based on rule type
        query = ruleType switch
        {
            CommissionRuleType.Category => query.Where(r => r.CategoryId == categoryId),
            CommissionRuleType.Seller => query.Where(r => r.StoreId == storeId),
            _ => query // Global rules have no additional scope filter
        };

        // Exclude the rule being updated
        if (excludeRuleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRuleId.Value);
        }

        var existingRules = await query.ToListAsync(cancellationToken);

        // Check for date overlap in memory (more complex logic)
        return existingRules
            .Where(r => DateRangesOverlap(r.EffectiveFrom, r.EffectiveTo, effectiveFrom, effectiveTo))
            .ToList();
    }

    /// <summary>
    /// Determines if two date ranges overlap.
    /// Null dates are treated as unbounded (negative/positive infinity).
    /// </summary>
    private static bool DateRangesOverlap(DateTime? from1, DateTime? to1, DateTime? from2, DateTime? to2)
    {
        // Treat null as unbounded
        // Range 1: [from1, to1], Range 2: [from2, to2]
        // Overlap occurs if: from1 <= to2 AND from2 <= to1

        // If from is null, treat as negative infinity (always <= any to)
        // If to is null, treat as positive infinity (any from is always <=)

        var from1Bounded = from1 ?? DateTime.MinValue;
        var to1Bounded = to1 ?? DateTime.MaxValue;
        var from2Bounded = from2 ?? DateTime.MinValue;
        var to2Bounded = to2 ?? DateTime.MaxValue;

        return from1Bounded <= to2Bounded && from2Bounded <= to1Bounded;
    }
}
