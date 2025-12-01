using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a security incident by ID.
/// </summary>
/// <param name="IncidentId">The incident ID.</param>
public record GetSecurityIncidentByIdQuery(Guid IncidentId);

/// <summary>
/// Query to get a security incident by incident number.
/// </summary>
/// <param name="IncidentNumber">The incident number.</param>
public record GetSecurityIncidentByNumberQuery(string IncidentNumber);

/// <summary>
/// Query to list security incidents with filters.
/// </summary>
/// <param name="Status">Optional status filter.</param>
/// <param name="Severity">Optional severity filter.</param>
/// <param name="IncidentType">Optional incident type filter.</param>
/// <param name="AssignedToUserId">Optional filter by assigned user.</param>
/// <param name="FromDate">Optional start date filter.</param>
/// <param name="ToDate">Optional end date filter.</param>
/// <param name="Skip">Number of records to skip.</param>
/// <param name="Take">Number of records to take.</param>
public record ListSecurityIncidentsQuery(
    SecurityIncidentStatus? Status = null,
    SecurityIncidentSeverity? Severity = null,
    string? IncidentType = null,
    Guid? AssignedToUserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Skip = 0,
    int Take = 50);

/// <summary>
/// Query to export security incidents for compliance review.
/// </summary>
/// <param name="FromDate">Start date of the export range.</param>
/// <param name="ToDate">End date of the export range.</param>
public record ExportSecurityIncidentsQuery(
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// Query to get security incidents affecting a specific user.
/// </summary>
/// <param name="AffectedUserId">The affected user ID.</param>
/// <param name="Skip">Number of records to skip.</param>
/// <param name="Take">Number of records to take.</param>
public record GetSecurityIncidentsByAffectedUserQuery(
    Guid AffectedUserId,
    int Skip = 0,
    int Take = 50);
