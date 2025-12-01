using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for security incident persistence operations.
/// </summary>
public interface ISecurityIncidentRepository
{
    /// <summary>
    /// Gets a security incident by its ID.
    /// </summary>
    /// <param name="id">The incident ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The incident if found, null otherwise.</returns>
    Task<SecurityIncident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a security incident by its incident number.
    /// </summary>
    /// <param name="incidentNumber">The incident number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The incident if found, null otherwise.</returns>
    Task<SecurityIncident?> GetByIncidentNumberAsync(string incidentNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available incident number.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next incident number in format INC-YYYY-NNNNN.</returns>
    Task<string> GetNextIncidentNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security incidents with optional filters and pagination.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="severity">Optional severity filter.</param>
    /// <param name="incidentType">Optional incident type filter.</param>
    /// <param name="assignedToUserId">Optional filter by assigned user.</param>
    /// <param name="fromDate">Optional start date filter (inclusive).</param>
    /// <param name="toDate">Optional end date filter (inclusive).</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of incidents matching the criteria.</returns>
    Task<IReadOnlyList<SecurityIncident>> GetAsync(
        SecurityIncidentStatus? status = null,
        SecurityIncidentSeverity? severity = null,
        string? incidentType = null,
        Guid? assignedToUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts security incidents matching the specified criteria.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="severity">Optional severity filter.</param>
    /// <param name="incidentType">Optional incident type filter.</param>
    /// <param name="assignedToUserId">Optional filter by assigned user.</param>
    /// <param name="fromDate">Optional start date filter (inclusive).</param>
    /// <param name="toDate">Optional end date filter (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of matching incidents.</returns>
    Task<int> CountAsync(
        SecurityIncidentStatus? status = null,
        SecurityIncidentSeverity? severity = null,
        string? incidentType = null,
        Guid? assignedToUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets incidents for export within a date range.
    /// </summary>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of incidents within the date range.</returns>
    Task<IReadOnlyList<SecurityIncident>> GetForExportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets incidents by affected user ID.
    /// </summary>
    /// <param name="affectedUserId">The affected user ID.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of incidents affecting the user.</returns>
    Task<IReadOnlyList<SecurityIncident>> GetByAffectedUserIdAsync(
        Guid affectedUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new security incident.
    /// </summary>
    /// <param name="incident">The incident to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(SecurityIncident incident, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing security incident.
    /// </summary>
    /// <param name="incident">The incident to update.</param>
    void Update(SecurityIncident incident);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
