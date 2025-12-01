using Microsoft.Extensions.Options;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Configuration options for security incident alerting.
/// Configure in appsettings.json under "SecurityIncidentAlerts" section.
/// </summary>
/// <example>
/// {
///   "SecurityIncidentAlerts": {
///     "AlertThreshold": "High",
///     "SecurityContactEmails": ["security@example.com", "soc@example.com"]
///   }
/// }
/// </example>
public sealed class SecurityIncidentAlertOptions : ISecurityIncidentAlertOptions
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "SecurityIncidentAlerts";

    /// <summary>
    /// The minimum severity level that triggers alert notifications.
    /// Default is High.
    /// </summary>
    public SecurityIncidentSeverity AlertThreshold { get; set; } = SecurityIncidentSeverity.High;

    /// <summary>
    /// Email addresses of security contacts to notify when alerts are triggered.
    /// Default is an empty list - configure in appsettings for production.
    /// </summary>
    public List<string> SecurityContactEmailsList { get; set; } = new();

    /// <inheritdoc />
    IReadOnlyList<string> ISecurityIncidentAlertOptions.SecurityContactEmails => SecurityContactEmailsList;
}

/// <summary>
/// Wrapper to adapt IOptions&lt;SecurityIncidentAlertOptions&gt; to ISecurityIncidentAlertOptions.
/// </summary>
internal sealed class SecurityIncidentAlertOptionsAdapter : ISecurityIncidentAlertOptions
{
    private readonly SecurityIncidentAlertOptions _options;

    public SecurityIncidentAlertOptionsAdapter(IOptions<SecurityIncidentAlertOptions> options)
    {
        _options = options.Value;
    }

    public SecurityIncidentSeverity AlertThreshold => _options.AlertThreshold;

    public IReadOnlyList<string> SecurityContactEmails => _options.SecurityContactEmailsList;
}
