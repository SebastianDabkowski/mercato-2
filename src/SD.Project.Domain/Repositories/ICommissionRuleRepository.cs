using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for CommissionRule persistence operations.
/// </summary>
public interface ICommissionRuleRepository
{
    /// <summary>
    /// Gets a commission rule by ID.
    /// </summary>
    Task<CommissionRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active global commission rule.
    /// Returns null if no active global rule exists.
    /// </summary>
    Task<CommissionRule?> GetActiveGlobalRuleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active commission rule for a specific category.
    /// Returns null if no active rule exists for the category.
    /// </summary>
    Task<CommissionRule?> GetActiveCategoryRuleAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active commission rule for a specific seller/store.
    /// Returns null if no active rule exists for the store.
    /// </summary>
    Task<CommissionRule?> GetActiveSellerRuleAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective commission rate for a specific store and category combination.
    /// Returns the most specific rule: seller > category > global > default.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="categoryId">Optional category ID. Can be null for uncategorized items.</param>
    /// <param name="effectiveDate">The date to check effectiveness. Defaults to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applicable commission rate or null if no rule applies.</returns>
    Task<decimal?> GetEffectiveRateAsync(
        Guid storeId,
        Guid? categoryId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all commission rules.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active commission rules.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all commission rules for a specific category.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all commission rules for a specific seller/store.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new commission rule.
    /// </summary>
    Task AddAsync(CommissionRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing commission rule.
    /// </summary>
    Task UpdateAsync(CommissionRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a commission rule.
    /// </summary>
    Task DeleteAsync(CommissionRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overlapping commission rules that may conflict with the given parameters.
    /// Used for validation before creating or updating rules.
    /// </summary>
    /// <param name="ruleType">The type of rule to check.</param>
    /// <param name="categoryId">Category ID for category-specific rules.</param>
    /// <param name="storeId">Store ID for seller-specific rules.</param>
    /// <param name="effectiveFrom">Start of effective period.</param>
    /// <param name="effectiveTo">End of effective period.</param>
    /// <param name="excludeRuleId">Optional rule ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of conflicting rules with overlapping effective dates.</returns>
    Task<IReadOnlyList<CommissionRule>> GetOverlappingRulesAsync(
        CommissionRuleType ruleType,
        Guid? categoryId,
        Guid? storeId,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default);
}
