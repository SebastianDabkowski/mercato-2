using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for feature flag persistence operations.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>
    /// Gets a feature flag by its unique identifier.
    /// </summary>
    Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a feature flag by its key.
    /// </summary>
    Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    Task<IReadOnlyCollection<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags with pagination.
    /// </summary>
    Task<(IReadOnlyCollection<FeatureFlag> Items, int TotalCount)> GetAllPagedAsync(
        string? searchTerm = null,
        FeatureFlagStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets environment configurations for a feature flag.
    /// </summary>
    Task<IReadOnlyCollection<FeatureFlagEnvironment>> GetEnvironmentsByFlagIdAsync(
        Guid featureFlagId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific environment configuration.
    /// </summary>
    Task<FeatureFlagEnvironment?> GetEnvironmentAsync(
        Guid featureFlagId,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a feature flag.
    /// </summary>
    Task<IReadOnlyCollection<FeatureFlagAuditLog>> GetAuditLogsByFlagIdAsync(
        Guid featureFlagId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit logs with optional filters.
    /// </summary>
    Task<(IReadOnlyCollection<FeatureFlagAuditLog> Items, int TotalCount)> GetAuditLogsAsync(
        Guid? featureFlagId = null,
        Guid? userId = null,
        FeatureFlagAuditAction? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new feature flag.
    /// </summary>
    Task AddAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new environment configuration.
    /// </summary>
    Task AddEnvironmentAsync(FeatureFlagEnvironment environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddAuditLogAsync(FeatureFlagAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a feature flag.
    /// </summary>
    void Update(FeatureFlag featureFlag);

    /// <summary>
    /// Updates an environment configuration.
    /// </summary>
    void UpdateEnvironment(FeatureFlagEnvironment environment);

    /// <summary>
    /// Deletes a feature flag and all its related data.
    /// </summary>
    void Delete(FeatureFlag featureFlag);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
