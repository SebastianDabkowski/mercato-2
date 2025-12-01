using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for SLA configuration persistence operations.
/// </summary>
public interface ISlaConfigurationRepository
{
    /// <summary>
    /// Gets an SLA configuration by ID.
    /// </summary>
    Task<SlaConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the SLA configuration for a specific case category.
    /// </summary>
    Task<SlaConfiguration?> GetByCategoryAsync(SlaCaseCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SLA configurations.
    /// </summary>
    Task<IReadOnlyList<SlaConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective SLA configuration for a case type.
    /// Returns the specific category config if available and enabled, otherwise the default.
    /// </summary>
    Task<SlaConfiguration?> GetEffectiveConfigAsync(SlaCaseCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an SLA configuration exists for the specified category.
    /// </summary>
    Task<bool> ExistsForCategoryAsync(SlaCaseCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new SLA configuration.
    /// </summary>
    Task AddAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing SLA configuration.
    /// </summary>
    Task UpdateAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an SLA configuration.
    /// </summary>
    Task DeleteAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
