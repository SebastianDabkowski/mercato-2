namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a security incident in the system.
/// Tracks detected security events that require investigation and response.
/// </summary>
public class SecurityIncident
{
    /// <summary>
    /// Maximum length for IP address fields.
    /// Supports IPv4 (max 15 chars) and IPv6 (max 39 chars) with some buffer.
    /// </summary>
    private const int MaxIpAddressLength = 45;

    /// <summary>
    /// Maximum length for user agent strings.
    /// </summary>
    private const int MaxUserAgentLength = 512;

    /// <summary>
    /// Unique identifier for the security incident.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Human-readable incident number for reference (e.g., INC-2024-00001).
    /// </summary>
    public string IncidentNumber { get; private set; } = default!;

    /// <summary>
    /// The type/category of the incident (e.g., "MultipleFailedLogins", "SuspiciousApiUsage", "DataAccessAnomaly").
    /// </summary>
    public string IncidentType { get; private set; } = default!;

    /// <summary>
    /// The detection rule that triggered this incident.
    /// </summary>
    public string DetectionRule { get; private set; } = default!;

    /// <summary>
    /// The severity level of the incident.
    /// </summary>
    public SecurityIncidentSeverity Severity { get; private set; }

    /// <summary>
    /// Current status of the incident in the workflow.
    /// </summary>
    public SecurityIncidentStatus Status { get; private set; }

    /// <summary>
    /// Brief title describing the incident.
    /// </summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// Detailed description of the incident.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The source system or component that detected the incident.
    /// </summary>
    public string Source { get; private set; } = default!;

    /// <summary>
    /// Optional ID of the user associated with the incident (e.g., affected user or attacker).
    /// </summary>
    public Guid? AffectedUserId { get; private set; }

    /// <summary>
    /// Optional ID of the resource affected by the incident.
    /// </summary>
    public Guid? AffectedResourceId { get; private set; }

    /// <summary>
    /// Type of the affected resource (e.g., "User", "Order", "Store").
    /// </summary>
    public string? AffectedResourceType { get; private set; }

    /// <summary>
    /// IP address associated with the incident.
    /// </summary>
    public string? SourceIpAddress { get; private set; }

    /// <summary>
    /// User agent string associated with the incident.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// ID of the security user assigned to investigate this incident.
    /// </summary>
    public Guid? AssignedToUserId { get; private set; }

    /// <summary>
    /// Resolution notes when the incident is resolved or marked as false positive.
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// UTC timestamp when the incident was detected.
    /// </summary>
    public DateTime DetectedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the incident was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the incident was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// Collection of status history entries for this incident.
    /// </summary>
    private readonly List<SecurityIncidentStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<SecurityIncidentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private SecurityIncident()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new security incident.
    /// </summary>
    /// <param name="incidentNumber">Human-readable incident number.</param>
    /// <param name="incidentType">The type/category of incident.</param>
    /// <param name="detectionRule">The rule that triggered detection.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="title">Brief title describing the incident.</param>
    /// <param name="source">The source system that detected the incident.</param>
    /// <param name="description">Detailed description of the incident.</param>
    /// <param name="affectedUserId">Optional affected user ID.</param>
    /// <param name="affectedResourceId">Optional affected resource ID.</param>
    /// <param name="affectedResourceType">Type of the affected resource.</param>
    /// <param name="sourceIpAddress">IP address associated with the incident.</param>
    /// <param name="userAgent">User agent string.</param>
    public SecurityIncident(
        string incidentNumber,
        string incidentType,
        string detectionRule,
        SecurityIncidentSeverity severity,
        string title,
        string source,
        string? description = null,
        Guid? affectedUserId = null,
        Guid? affectedResourceId = null,
        string? affectedResourceType = null,
        string? sourceIpAddress = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(incidentNumber))
        {
            throw new ArgumentException("Incident number is required.", nameof(incidentNumber));
        }

        if (string.IsNullOrWhiteSpace(incidentType))
        {
            throw new ArgumentException("Incident type is required.", nameof(incidentType));
        }

        if (string.IsNullOrWhiteSpace(detectionRule))
        {
            throw new ArgumentException("Detection rule is required.", nameof(detectionRule));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        Id = Guid.NewGuid();
        IncidentNumber = incidentNumber.Trim();
        IncidentType = incidentType.Trim();
        DetectionRule = detectionRule.Trim();
        Severity = severity;
        Status = SecurityIncidentStatus.Open;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Source = source.Trim();
        AffectedUserId = affectedUserId;
        AffectedResourceId = affectedResourceId;
        AffectedResourceType = affectedResourceType?.Trim();
        SourceIpAddress = sourceIpAddress?.Length > MaxIpAddressLength ? sourceIpAddress[..MaxIpAddressLength] : sourceIpAddress;
        UserAgent = userAgent?.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
        DetectedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Add initial status history entry
        _statusHistory.Add(new SecurityIncidentStatusHistory(
            Id,
            SecurityIncidentStatus.Open,
            null, // System created
            "Incident detected and logged."));
    }

    /// <summary>
    /// Updates the status of the incident with an actor and optional notes.
    /// </summary>
    /// <param name="newStatus">The new status.</param>
    /// <param name="actorUserId">The ID of the user making the change.</param>
    /// <param name="notes">Optional notes about the status change.</param>
    /// <returns>List of validation errors. Empty if successful.</returns>
    public IReadOnlyList<string> UpdateStatus(SecurityIncidentStatus newStatus, Guid actorUserId, string? notes = null)
    {
        var errors = new List<string>();

        if (actorUserId == Guid.Empty)
        {
            errors.Add("Actor user ID is required.");
            return errors;
        }

        // Validate status transitions
        if (!CanTransitionTo(newStatus))
        {
            errors.Add($"Cannot transition from {Status} to {newStatus}.");
            return errors;
        }

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        // Track resolution timestamp
        if (newStatus is SecurityIncidentStatus.Resolved or SecurityIncidentStatus.FalsePositive)
        {
            ResolvedAt = DateTime.UtcNow;
        }

        // Add status history entry
        _statusHistory.Add(new SecurityIncidentStatusHistory(
            Id,
            newStatus,
            actorUserId,
            notes,
            previousStatus));

        return errors;
    }

    /// <summary>
    /// Checks if a status transition is allowed.
    /// </summary>
    /// <param name="targetStatus">The target status.</param>
    /// <returns>True if the transition is allowed.</returns>
    public bool CanTransitionTo(SecurityIncidentStatus targetStatus)
    {
        // Same status is a no-op, allow it
        if (Status == targetStatus)
        {
            return true;
        }

        // Cannot transition from closed statuses
        if (Status is SecurityIncidentStatus.Resolved or SecurityIncidentStatus.FalsePositive)
        {
            return false;
        }

        // Define valid transitions
        return Status switch
        {
            SecurityIncidentStatus.Open => targetStatus is SecurityIncidentStatus.Triaged or SecurityIncidentStatus.InInvestigation or SecurityIncidentStatus.FalsePositive,
            SecurityIncidentStatus.Triaged => targetStatus is SecurityIncidentStatus.InInvestigation or SecurityIncidentStatus.Resolved or SecurityIncidentStatus.FalsePositive,
            SecurityIncidentStatus.InInvestigation => targetStatus is SecurityIncidentStatus.Contained or SecurityIncidentStatus.Resolved or SecurityIncidentStatus.FalsePositive,
            SecurityIncidentStatus.Contained => targetStatus is SecurityIncidentStatus.Resolved or SecurityIncidentStatus.InInvestigation,
            _ => false
        };
    }

    /// <summary>
    /// Assigns the incident to a security user for investigation.
    /// </summary>
    /// <param name="userId">The ID of the user to assign.</param>
    public void AssignTo(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        AssignedToUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unassigns the incident.
    /// </summary>
    public void Unassign()
    {
        AssignedToUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the severity level.
    /// </summary>
    /// <param name="severity">The new severity level.</param>
    public void UpdateSeverity(SecurityIncidentSeverity severity)
    {
        Severity = severity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets resolution notes when closing the incident.
    /// </summary>
    /// <param name="notes">The resolution notes.</param>
    public void SetResolutionNotes(string? notes)
    {
        ResolutionNotes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the description of the incident.
    /// </summary>
    /// <param name="description">The new description.</param>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the incident is in a closed status (Resolved or FalsePositive).
    /// </summary>
    public bool IsClosed => Status is SecurityIncidentStatus.Resolved or SecurityIncidentStatus.FalsePositive;

    /// <summary>
    /// Checks if the incident requires alerting based on severity threshold.
    /// </summary>
    /// <param name="alertThreshold">The minimum severity that triggers an alert.</param>
    /// <returns>True if the incident severity meets or exceeds the threshold.</returns>
    public bool RequiresAlert(SecurityIncidentSeverity alertThreshold)
    {
        return Severity >= alertThreshold;
    }
}
