namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines severity levels for security incidents.
/// Higher levels indicate more critical incidents requiring faster response.
/// </summary>
public enum SecurityIncidentSeverity
{
    /// <summary>
    /// Low severity - minimal impact, no immediate action required.
    /// Example: Single failed login attempt from unknown IP.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - potential security concern requiring investigation.
    /// Example: Multiple failed login attempts, unusual API usage patterns.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - confirmed security concern requiring prompt response.
    /// Example: Suspected account compromise, data access anomaly.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - active security breach or imminent threat.
    /// Example: Confirmed data breach, active attack in progress.
    /// </summary>
    Critical = 3
}
