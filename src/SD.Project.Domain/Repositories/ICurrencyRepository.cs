using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for Currency persistence operations.
/// </summary>
public interface ICurrencyRepository
{
    /// <summary>
    /// Gets a currency by ID.
    /// </summary>
    Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a currency by its ISO code.
    /// </summary>
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled currencies.
    /// </summary>
    Task<IReadOnlyList<Currency>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current base currency.
    /// </summary>
    Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a currency code already exists.
    /// </summary>
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new currency.
    /// </summary>
    Task AddAsync(Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    Task UpdateAsync(Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
