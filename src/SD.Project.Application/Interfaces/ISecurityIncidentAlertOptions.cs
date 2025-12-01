using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Configuration options for security incident alerting.
/// Implement this interface to provide custom alert configuration.
/// </summary>
public interface ISecurityIncidentAlertOptions
{
    /// <summary>
    /// The minimum severity level that triggers alert notifications.
    /// Incidents with this severity or higher will send alerts to security contacts.
    /// Default should be High.
    /// </summary>
    SecurityIncidentSeverity AlertThreshold { get; }

    /// <summary>
    /// Email addresses of security contacts to notify when alerts are triggered.
    /// These contacts receive email notifications for high-severity incidents.
    /// </summary>
    IReadOnlyList<string> SecurityContactEmails { get; }
}
