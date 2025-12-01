using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for VatRule persistence operations.
/// </summary>
public interface IVatRuleRepository
{
    /// <summary>
    /// Gets a VAT rule by ID.
    /// </summary>
    Task<VatRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all VAT rules.
    /// </summary>
    Task<IReadOnlyList<VatRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active VAT rules.
    /// </summary>
    Task<IReadOnlyList<VatRule>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all VAT rules for a specific country.
    /// </summary>
    Task<IReadOnlyList<VatRule>> GetByCountryAsync(string countryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective VAT rate for a specific country and optional category.
    /// Returns the most specific rule: category-specific > country-wide.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <param name="effectiveDate">The date to check effectiveness. Defaults to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applicable VAT rule or null if no rule applies.</returns>
    Task<VatRule?> GetEffectiveRuleAsync(
        string countryCode,
        Guid? categoryId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all VAT rules for a specific category.
    /// </summary>
    Task<IReadOnlyList<VatRule>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overlapping VAT rules that may conflict with the given parameters.
    /// Used for validation before creating or updating rules.
    /// </summary>
    /// <param name="countryCode">Country code for the rule.</param>
    /// <param name="categoryId">Category ID (null for country-wide rules).</param>
    /// <param name="effectiveFrom">Start of effective period.</param>
    /// <param name="effectiveTo">End of effective period.</param>
    /// <param name="excludeRuleId">Optional rule ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of conflicting rules with overlapping effective dates.</returns>
    Task<IReadOnlyList<VatRule>> GetOverlappingRulesAsync(
        string countryCode,
        Guid? categoryId,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct country codes that have VAT rules defined.
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctCountryCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new VAT rule.
    /// </summary>
    Task AddAsync(VatRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing VAT rule.
    /// </summary>
    Task UpdateAsync(VatRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a VAT rule.
    /// </summary>
    Task DeleteAsync(VatRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
