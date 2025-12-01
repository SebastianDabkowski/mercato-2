using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for integration persistence operations.
/// </summary>
public interface IIntegrationRepository
{
    /// <summary>
    /// Gets an integration by its ID.
    /// </summary>
    Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all integrations.
    /// </summary>
    Task<IReadOnlyCollection<Integration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integrations by type.
    /// </summary>
    Task<IReadOnlyCollection<Integration>> GetByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integrations by environment.
    /// </summary>
    Task<IReadOnlyCollection<Integration>> GetByEnvironmentAsync(IntegrationEnvironment environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active integrations of a specific type.
    /// </summary>
    Task<IReadOnlyCollection<Integration>> GetActiveByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integrations with optional filtering and pagination.
    /// </summary>
    Task<(IReadOnlyCollection<Integration> Items, int TotalCount)> GetPagedAsync(
        IntegrationType? type = null,
        IntegrationStatus? status = null,
        IntegrationEnvironment? environment = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new integration.
    /// </summary>
    Task AddAsync(Integration integration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing integration.
    /// </summary>
    void Update(Integration integration);

    /// <summary>
    /// Deletes an integration.
    /// </summary>
    void Delete(Integration integration);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
