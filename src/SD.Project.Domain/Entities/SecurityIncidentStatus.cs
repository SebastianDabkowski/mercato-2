namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the workflow status of a security incident.
/// </summary>
public enum SecurityIncidentStatus
{
    /// <summary>
    /// Incident has been detected and logged but not yet reviewed.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Incident has been reviewed and prioritized by security team.
    /// </summary>
    Triaged = 1,

    /// <summary>
    /// Incident is actively being investigated.
    /// </summary>
    InInvestigation = 2,

    /// <summary>
    /// Incident has been contained and remediation is in progress.
    /// </summary>
    Contained = 3,

    /// <summary>
    /// Incident has been resolved and closed.
    /// </summary>
    Resolved = 4,

    /// <summary>
    /// Incident was determined to be a false positive.
    /// </summary>
    FalsePositive = 5
}
