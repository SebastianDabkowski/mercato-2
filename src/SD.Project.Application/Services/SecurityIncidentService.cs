using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing security incidents.
/// Handles incident creation, status updates, and reporting.
/// </summary>
public sealed class SecurityIncidentService
{
    private readonly ISecurityIncidentRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SecurityIncidentService> _logger;

    /// <summary>
    /// Configuration for alert thresholds. In production, this should be externalized.
    /// </summary>
    private const SecurityIncidentSeverity AlertThreshold = SecurityIncidentSeverity.High;

    /// <summary>
    /// Placeholder for security contact emails. In production, load from configuration.
    /// </summary>
    private static readonly string[] SecurityContacts = { "security@mercato.example" };

    public SecurityIncidentService(
        ISecurityIncidentRepository repository,
        IEmailSender emailSender,
        ILogger<SecurityIncidentService> logger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new security incident and sends alerts if severity meets threshold.
    /// </summary>
    public async Task<CreateSecurityIncidentResultDto> HandleAsync(
        CreateSecurityIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate required fields
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateSecurityIncidentResultDto.Failed(validationErrors);
        }

        try
        {
            // Get next incident number
            var incidentNumber = await _repository.GetNextIncidentNumberAsync(cancellationToken);

            // Create the incident
            var incident = new SecurityIncident(
                incidentNumber,
                command.IncidentType.Trim(),
                command.DetectionRule.Trim(),
                command.Severity,
                command.Title.Trim(),
                command.Source.Trim(),
                command.Description,
                command.AffectedUserId,
                command.AffectedResourceId,
                command.AffectedResourceType,
                command.SourceIpAddress,
                command.UserAgent);

            await _repository.AddAsync(incident, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Security incident {IncidentNumber} created: {Title} (Severity: {Severity})",
                incident.IncidentNumber,
                incident.Title,
                incident.Severity);

            // Send alert if severity meets threshold
            var alertSent = false;
            if (incident.RequiresAlert(AlertThreshold))
            {
                alertSent = await SendAlertAsync(incident, cancellationToken);
            }

            return CreateSecurityIncidentResultDto.Succeeded(MapToDto(incident), alertSent);
        }
        catch (ArgumentException ex)
        {
            return CreateSecurityIncidentResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates the status of a security incident.
    /// </summary>
    public async Task<UpdateSecurityIncidentStatusResultDto> HandleAsync(
        UpdateSecurityIncidentStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await _repository.GetByIdAsync(command.IncidentId, cancellationToken);
        if (incident is null)
        {
            return UpdateSecurityIncidentStatusResultDto.Failed("Security incident not found.");
        }

        var errors = incident.UpdateStatus(command.NewStatus, command.ActorUserId, command.Notes);
        if (errors.Count > 0)
        {
            return UpdateSecurityIncidentStatusResultDto.Failed(errors);
        }

        _repository.Update(incident);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Security incident {IncidentNumber} status updated to {Status} by user {UserId}",
            incident.IncidentNumber,
            command.NewStatus,
            command.ActorUserId);

        return UpdateSecurityIncidentStatusResultDto.Succeeded(MapToDto(incident));
    }

    /// <summary>
    /// Assigns a security incident to a user.
    /// </summary>
    public async Task<UpdateSecurityIncidentStatusResultDto> HandleAsync(
        AssignSecurityIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await _repository.GetByIdAsync(command.IncidentId, cancellationToken);
        if (incident is null)
        {
            return UpdateSecurityIncidentStatusResultDto.Failed("Security incident not found.");
        }

        try
        {
            incident.AssignTo(command.AssignToUserId);
            _repository.Update(incident);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Security incident {IncidentNumber} assigned to user {AssignedUserId} by {ActorUserId}",
                incident.IncidentNumber,
                command.AssignToUserId,
                command.ActorUserId);

            return UpdateSecurityIncidentStatusResultDto.Succeeded(MapToDto(incident));
        }
        catch (ArgumentException ex)
        {
            return UpdateSecurityIncidentStatusResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates the severity of a security incident.
    /// </summary>
    public async Task<UpdateSecurityIncidentStatusResultDto> HandleAsync(
        UpdateSecurityIncidentSeverityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await _repository.GetByIdAsync(command.IncidentId, cancellationToken);
        if (incident is null)
        {
            return UpdateSecurityIncidentStatusResultDto.Failed("Security incident not found.");
        }

        var previousSeverity = incident.Severity;
        incident.UpdateSeverity(command.NewSeverity);

        _repository.Update(incident);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Security incident {IncidentNumber} severity changed from {PreviousSeverity} to {NewSeverity} by {ActorUserId}",
            incident.IncidentNumber,
            previousSeverity,
            command.NewSeverity,
            command.ActorUserId);

        // Send alert if severity was escalated to meet threshold
        if (previousSeverity < AlertThreshold && incident.RequiresAlert(AlertThreshold))
        {
            await SendAlertAsync(incident, cancellationToken);
        }

        return UpdateSecurityIncidentStatusResultDto.Succeeded(MapToDto(incident));
    }

    /// <summary>
    /// Gets a security incident by ID with status history.
    /// </summary>
    public async Task<SecurityIncidentDetailDto?> HandleAsync(
        GetSecurityIncidentByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var incident = await _repository.GetByIdAsync(query.IncidentId, cancellationToken);
        if (incident is null)
        {
            return null;
        }

        return MapToDetailDto(incident);
    }

    /// <summary>
    /// Gets a security incident by incident number with status history.
    /// </summary>
    public async Task<SecurityIncidentDetailDto?> HandleAsync(
        GetSecurityIncidentByNumberQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var incident = await _repository.GetByIncidentNumberAsync(query.IncidentNumber, cancellationToken);
        if (incident is null)
        {
            return null;
        }

        return MapToDetailDto(incident);
    }

    /// <summary>
    /// Lists security incidents with optional filters.
    /// </summary>
    public async Task<IReadOnlyList<SecurityIncidentDto>> HandleAsync(
        ListSecurityIncidentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var incidents = await _repository.GetAsync(
            query.Status,
            query.Severity,
            query.IncidentType,
            query.AssignedToUserId,
            query.FromDate,
            query.ToDate,
            query.Skip,
            query.Take,
            cancellationToken);

        return incidents.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Gets security incidents affecting a specific user.
    /// </summary>
    public async Task<IReadOnlyList<SecurityIncidentDto>> HandleAsync(
        GetSecurityIncidentsByAffectedUserQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var incidents = await _repository.GetByAffectedUserIdAsync(
            query.AffectedUserId,
            query.Skip,
            query.Take,
            cancellationToken);

        return incidents.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Exports security incidents for compliance review.
    /// </summary>
    public async Task<SecurityIncidentExportResultDto> HandleAsync(
        ExportSecurityIncidentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.FromDate > query.ToDate)
        {
            return SecurityIncidentExportResultDto.Failed("From date must be before or equal to To date.");
        }

        var incidents = await _repository.GetForExportAsync(
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var exportDtos = incidents.Select(i => new SecurityIncidentExportDto(
            i.IncidentNumber,
            i.IncidentType,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.Title,
            i.DetectedAt,
            i.ResolvedAt,
            i.ResolutionNotes)).ToArray();

        _logger.LogInformation(
            "Exported {Count} security incidents for compliance review from {FromDate} to {ToDate}",
            exportDtos.Length,
            query.FromDate,
            query.ToDate);

        return SecurityIncidentExportResultDto.Succeeded(exportDtos, query.FromDate, query.ToDate);
    }

    /// <summary>
    /// Counts security incidents matching criteria.
    /// </summary>
    public async Task<int> CountAsync(
        SecurityIncidentStatus? status = null,
        SecurityIncidentSeverity? severity = null,
        string? incidentType = null,
        Guid? assignedToUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return await _repository.CountAsync(
            status,
            severity,
            incidentType,
            assignedToUserId,
            fromDate,
            toDate,
            cancellationToken);
    }

    private async Task<bool> SendAlertAsync(SecurityIncident incident, CancellationToken cancellationToken)
    {
        try
        {
            var subject = $"[SECURITY ALERT] {incident.Severity} - {incident.IncidentNumber}: {incident.Title}";
            var htmlBody = $@"<h2>Security Incident Detected</h2>
<p>A security incident has been detected that requires attention.</p>
<table>
<tr><td><strong>Incident Number:</strong></td><td>{incident.IncidentNumber}</td></tr>
<tr><td><strong>Title:</strong></td><td>{incident.Title}</td></tr>
<tr><td><strong>Severity:</strong></td><td>{incident.Severity}</td></tr>
<tr><td><strong>Type:</strong></td><td>{incident.IncidentType}</td></tr>
<tr><td><strong>Detection Rule:</strong></td><td>{incident.DetectionRule}</td></tr>
<tr><td><strong>Source:</strong></td><td>{incident.Source}</td></tr>
<tr><td><strong>Detected At:</strong></td><td>{incident.DetectedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
</table>
<h3>Description</h3>
<p>{incident.Description}</p>
<p>Please review and respond to this incident according to the security response playbook.</p>";

            var textBody = $@"Security Incident Detected

A security incident has been detected that requires attention.

Incident Number: {incident.IncidentNumber}
Title: {incident.Title}
Severity: {incident.Severity}
Type: {incident.IncidentType}
Detection Rule: {incident.DetectionRule}
Source: {incident.Source}
Detected At: {incident.DetectedAt:yyyy-MM-dd HH:mm:ss} UTC

Description:
{incident.Description}

Please review and respond to this incident according to the security response playbook.";

            foreach (var contact in SecurityContacts)
            {
                var message = new EmailMessage(
                    contact,
                    subject,
                    htmlBody,
                    textBody,
                    "SecurityAlert",
                    "en-US");
                await _emailSender.SendAsync(message, cancellationToken);
            }

            _logger.LogInformation(
                "Security alert sent for incident {IncidentNumber} to {ContactCount} contacts",
                incident.IncidentNumber,
                SecurityContacts.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send security alert for incident {IncidentNumber}",
                incident.IncidentNumber);
            return false;
        }
    }

    private static IReadOnlyList<string> ValidateCreateCommand(CreateSecurityIncidentCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.IncidentType))
        {
            errors.Add("Incident type is required.");
        }

        if (string.IsNullOrWhiteSpace(command.DetectionRule))
        {
            errors.Add("Detection rule is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            errors.Add("Title is required.");
        }
        else if (command.Title.Trim().Length > 500)
        {
            errors.Add("Title cannot exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Source))
        {
            errors.Add("Source is required.");
        }

        return errors;
    }

    private static SecurityIncidentDto MapToDto(SecurityIncident incident)
    {
        return new SecurityIncidentDto(
            incident.Id,
            incident.IncidentNumber,
            incident.IncidentType,
            incident.DetectionRule,
            incident.Severity,
            incident.Status,
            incident.Title,
            incident.Description,
            incident.Source,
            incident.AffectedUserId,
            incident.AffectedResourceId,
            incident.AffectedResourceType,
            incident.SourceIpAddress,
            incident.AssignedToUserId,
            incident.ResolutionNotes,
            incident.DetectedAt,
            incident.UpdatedAt,
            incident.ResolvedAt);
    }

    private static SecurityIncidentDetailDto MapToDetailDto(SecurityIncident incident)
    {
        var statusHistory = incident.StatusHistory
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new SecurityIncidentStatusHistoryDto(
                h.Id,
                h.Status,
                h.PreviousStatus,
                h.ChangedByUserId,
                h.Notes,
                h.ChangedAt))
            .ToArray();

        return new SecurityIncidentDetailDto(MapToDto(incident), statusHistory);
    }
}
