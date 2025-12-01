using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for a security incident.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="IncidentNumber">Human-readable incident number.</param>
/// <param name="IncidentType">Type/category of the incident.</param>
/// <param name="DetectionRule">The detection rule that triggered the incident.</param>
/// <param name="Severity">Severity level.</param>
/// <param name="Status">Current status.</param>
/// <param name="Title">Brief title.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="Source">Source system that detected the incident.</param>
/// <param name="AffectedUserId">Optional affected user ID.</param>
/// <param name="AffectedResourceId">Optional affected resource ID.</param>
/// <param name="AffectedResourceType">Type of affected resource.</param>
/// <param name="SourceIpAddress">IP address associated with the incident.</param>
/// <param name="AssignedToUserId">ID of the assigned security user.</param>
/// <param name="ResolutionNotes">Resolution notes.</param>
/// <param name="DetectedAt">When the incident was detected.</param>
/// <param name="UpdatedAt">When the incident was last updated.</param>
/// <param name="ResolvedAt">When the incident was resolved.</param>
public record SecurityIncidentDto(
    Guid Id,
    string IncidentNumber,
    string IncidentType,
    string DetectionRule,
    SecurityIncidentSeverity Severity,
    SecurityIncidentStatus Status,
    string Title,
    string Description,
    string Source,
    Guid? AffectedUserId,
    Guid? AffectedResourceId,
    string? AffectedResourceType,
    string? SourceIpAddress,
    Guid? AssignedToUserId,
    string? ResolutionNotes,
    DateTime DetectedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);

/// <summary>
/// Data transfer object for a security incident status history entry.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Status">The status that was set.</param>
/// <param name="PreviousStatus">The previous status.</param>
/// <param name="ChangedByUserId">ID of the user who made the change.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="ChangedAt">When the change occurred.</param>
public record SecurityIncidentStatusHistoryDto(
    Guid Id,
    SecurityIncidentStatus Status,
    SecurityIncidentStatus? PreviousStatus,
    Guid? ChangedByUserId,
    string? Notes,
    DateTime ChangedAt);

/// <summary>
/// Detailed view of a security incident including status history.
/// </summary>
/// <param name="Incident">The incident details.</param>
/// <param name="StatusHistory">List of status history entries.</param>
public record SecurityIncidentDetailDto(
    SecurityIncidentDto Incident,
    IReadOnlyList<SecurityIncidentStatusHistoryDto> StatusHistory);

/// <summary>
/// Result DTO for creating a security incident.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="Incident">The created incident (if successful).</param>
/// <param name="Errors">Validation errors (if failed).</param>
/// <param name="AlertSent">Whether an alert was sent for this incident.</param>
public record CreateSecurityIncidentResultDto(
    bool IsSuccess,
    SecurityIncidentDto? Incident,
    IReadOnlyList<string> Errors,
    bool AlertSent)
{
    public static CreateSecurityIncidentResultDto Succeeded(SecurityIncidentDto incident, bool alertSent)
        => new(true, incident, Array.Empty<string>(), alertSent);

    public static CreateSecurityIncidentResultDto Failed(params string[] errors)
        => new(false, null, errors, false);

    public static CreateSecurityIncidentResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors, false);
}

/// <summary>
/// Result DTO for updating a security incident status.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="Incident">The updated incident (if successful).</param>
/// <param name="Errors">Validation errors (if failed).</param>
public record UpdateSecurityIncidentStatusResultDto(
    bool IsSuccess,
    SecurityIncidentDto? Incident,
    IReadOnlyList<string> Errors)
{
    public static UpdateSecurityIncidentStatusResultDto Succeeded(SecurityIncidentDto incident)
        => new(true, incident, Array.Empty<string>());

    public static UpdateSecurityIncidentStatusResultDto Failed(params string[] errors)
        => new(false, null, errors);

    public static UpdateSecurityIncidentStatusResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors);
}

/// <summary>
/// Summary DTO for incident reports and exports.
/// </summary>
/// <param name="IncidentNumber">Human-readable incident number.</param>
/// <param name="IncidentType">Type/category of the incident.</param>
/// <param name="Severity">Severity level.</param>
/// <param name="Status">Current status.</param>
/// <param name="Title">Brief title.</param>
/// <param name="DetectedAt">When the incident was detected.</param>
/// <param name="ResolvedAt">When the incident was resolved.</param>
/// <param name="ResolutionNotes">Resolution notes.</param>
public record SecurityIncidentExportDto(
    string IncidentNumber,
    string IncidentType,
    string Severity,
    string Status,
    string Title,
    DateTime DetectedAt,
    DateTime? ResolvedAt,
    string? ResolutionNotes);

/// <summary>
/// Result DTO for exporting incident reports.
/// </summary>
/// <param name="IsSuccess">Whether the export succeeded.</param>
/// <param name="Incidents">List of incidents for export.</param>
/// <param name="TotalCount">Total number of incidents.</param>
/// <param name="FromDate">Start date of the export range.</param>
/// <param name="ToDate">End date of the export range.</param>
/// <param name="Errors">Validation errors (if failed).</param>
public record SecurityIncidentExportResultDto(
    bool IsSuccess,
    IReadOnlyList<SecurityIncidentExportDto> Incidents,
    int TotalCount,
    DateTime FromDate,
    DateTime ToDate,
    IReadOnlyList<string> Errors)
{
    public static SecurityIncidentExportResultDto Succeeded(
        IReadOnlyList<SecurityIncidentExportDto> incidents,
        DateTime fromDate,
        DateTime toDate)
        => new(true, incidents, incidents.Count, fromDate, toDate, Array.Empty<string>());

    public static SecurityIncidentExportResultDto Failed(params string[] errors)
        => new(false, Array.Empty<SecurityIncidentExportDto>(), 0, default, default, errors);
}
