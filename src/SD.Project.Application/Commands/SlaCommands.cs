namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new SLA configuration.
/// </summary>
public sealed record CreateSlaConfigurationCommand(
    string Category,
    int FirstResponseHours,
    int ResolutionHours,
    string? Description = null);

/// <summary>
/// Command to update an existing SLA configuration.
/// </summary>
public sealed record UpdateSlaConfigurationCommand(
    Guid ConfigurationId,
    int FirstResponseHours,
    int ResolutionHours,
    string? Description = null);

/// <summary>
/// Command to enable an SLA configuration.
/// </summary>
public sealed record EnableSlaConfigurationCommand(Guid ConfigurationId);

/// <summary>
/// Command to disable an SLA configuration.
/// </summary>
public sealed record DisableSlaConfigurationCommand(Guid ConfigurationId);

/// <summary>
/// Command to delete an SLA configuration.
/// </summary>
public sealed record DeleteSlaConfigurationCommand(Guid ConfigurationId);

/// <summary>
/// Command to check for SLA breaches and flag cases that have exceeded deadlines.
/// This command can be triggered by a scheduled job or admin action.
/// </summary>
public sealed record CheckSlaBreachesCommand();
