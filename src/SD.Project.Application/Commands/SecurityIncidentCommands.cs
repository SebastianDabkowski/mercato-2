using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new security incident.
/// </summary>
/// <param name="IncidentType">The type/category of incident.</param>
/// <param name="DetectionRule">The rule that triggered detection.</param>
/// <param name="Severity">The severity level.</param>
/// <param name="Title">Brief title describing the incident.</param>
/// <param name="Source">The source system that detected the incident.</param>
/// <param name="Description">Detailed description of the incident.</param>
/// <param name="AffectedUserId">Optional affected user ID.</param>
/// <param name="AffectedResourceId">Optional affected resource ID.</param>
/// <param name="AffectedResourceType">Type of the affected resource.</param>
/// <param name="SourceIpAddress">IP address associated with the incident.</param>
/// <param name="UserAgent">User agent string.</param>
public record CreateSecurityIncidentCommand(
    string IncidentType,
    string DetectionRule,
    SecurityIncidentSeverity Severity,
    string Title,
    string Source,
    string? Description = null,
    Guid? AffectedUserId = null,
    Guid? AffectedResourceId = null,
    string? AffectedResourceType = null,
    string? SourceIpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to update the status of a security incident.
/// </summary>
/// <param name="IncidentId">The ID of the incident to update.</param>
/// <param name="NewStatus">The new status.</param>
/// <param name="ActorUserId">The ID of the user making the change.</param>
/// <param name="Notes">Optional notes about the status change.</param>
public record UpdateSecurityIncidentStatusCommand(
    Guid IncidentId,
    SecurityIncidentStatus NewStatus,
    Guid ActorUserId,
    string? Notes = null);

/// <summary>
/// Command to assign a security incident to a user.
/// </summary>
/// <param name="IncidentId">The ID of the incident to assign.</param>
/// <param name="AssignToUserId">The ID of the user to assign to.</param>
/// <param name="ActorUserId">The ID of the user making the assignment.</param>
public record AssignSecurityIncidentCommand(
    Guid IncidentId,
    Guid AssignToUserId,
    Guid ActorUserId);

/// <summary>
/// Command to update the severity of a security incident.
/// </summary>
/// <param name="IncidentId">The ID of the incident to update.</param>
/// <param name="NewSeverity">The new severity level.</param>
/// <param name="ActorUserId">The ID of the user making the change.</param>
public record UpdateSecurityIncidentSeverityCommand(
    Guid IncidentId,
    SecurityIncidentSeverity NewSeverity,
    Guid ActorUserId);
