using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for VatRuleHistory persistence operations.
/// </summary>
public interface IVatRuleHistoryRepository
{
    /// <summary>
    /// Gets all history entries for a specific VAT rule.
    /// </summary>
    Task<IReadOnlyList<VatRuleHistory>> GetByVatRuleIdAsync(Guid vatRuleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all history entries, optionally filtered by country code.
    /// </summary>
    Task<IReadOnlyList<VatRuleHistory>> GetAllAsync(string? countryCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history entries created by a specific user.
    /// </summary>
    Task<IReadOnlyList<VatRuleHistory>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history entries within a date range.
    /// </summary>
    Task<IReadOnlyList<VatRuleHistory>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new history entry.
    /// </summary>
    Task AddAsync(VatRuleHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
