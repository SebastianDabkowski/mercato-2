using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display feature flag data on Razor Pages.
/// </summary>
public sealed record FeatureFlagViewModel(
    Guid Id,
    string Key,
    string Name,
    string Description,
    FeatureFlagStatus Status,
    bool GlobalOverride,
    int RolloutPercentage,
    string? TargetUserGroups,
    string? TargetUserIds,
    string? TargetSellerIds,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyCollection<FeatureFlagEnvironmentViewModel> Environments)
{
    /// <summary>
    /// Gets a display-friendly status string.
    /// </summary>
    public string StatusDisplay => Status switch
    {
        FeatureFlagStatus.Disabled => "Disabled",
        FeatureFlagStatus.Enabled => "Enabled",
        FeatureFlagStatus.Targeted => "Targeted",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets a CSS class for the status badge.
    /// </summary>
    public string StatusBadgeClass => Status switch
    {
        FeatureFlagStatus.Disabled => "bg-secondary",
        FeatureFlagStatus.Enabled => "bg-success",
        FeatureFlagStatus.Targeted => "bg-info",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets a description or placeholder if not set.
    /// </summary>
    public string DescriptionDisplay => string.IsNullOrWhiteSpace(Description) ? "—" : Description;

    /// <summary>
    /// Gets whether any targeting rules are configured.
    /// </summary>
    public bool HasTargetingRules =>
        RolloutPercentage > 0 ||
        !string.IsNullOrWhiteSpace(TargetUserGroups) ||
        !string.IsNullOrWhiteSpace(TargetUserIds) ||
        !string.IsNullOrWhiteSpace(TargetSellerIds);

    /// <summary>
    /// Gets a summary of targeting rules.
    /// </summary>
    public string TargetingSummary
    {
        get
        {
            var parts = new List<string>();

            if (RolloutPercentage > 0)
            {
                parts.Add($"{RolloutPercentage}% rollout");
            }

            if (!string.IsNullOrWhiteSpace(TargetUserGroups))
            {
                var count = TargetUserGroups.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                parts.Add($"{count} group{(count != 1 ? "s" : "")}");
            }

            if (!string.IsNullOrWhiteSpace(TargetUserIds))
            {
                var count = TargetUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                parts.Add($"{count} user{(count != 1 ? "s" : "")}");
            }

            if (!string.IsNullOrWhiteSpace(TargetSellerIds))
            {
                var count = TargetSellerIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                parts.Add($"{count} seller{(count != 1 ? "s" : "")}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "No targeting rules";
        }
    }
}

/// <summary>
/// View model for feature flag environment configuration.
/// </summary>
public sealed record FeatureFlagEnvironmentViewModel(
    Guid Id,
    Guid FeatureFlagId,
    string Environment,
    bool IsEnabled,
    int? RolloutPercentageOverride,
    Guid? LastModifiedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets a display-friendly environment name.
    /// </summary>
    public string EnvironmentDisplay => Environment switch
    {
        "development" => "Development",
        "test" => "Test",
        "staging" => "Staging",
        "production" => "Production",
        _ => string.IsNullOrEmpty(Environment) ? "Unknown" : char.ToUpper(Environment[0]) + Environment[1..]
    };

    /// <summary>
    /// Gets a CSS class for the environment badge.
    /// </summary>
    public string EnvironmentBadgeClass => Environment switch
    {
        "production" => "bg-danger",
        "staging" => "bg-warning text-dark",
        "test" => "bg-info",
        "development" => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets a status display string.
    /// </summary>
    public string StatusDisplay => IsEnabled ? "Enabled" : "Disabled";

    /// <summary>
    /// Gets a CSS class for the status indicator.
    /// </summary>
    public string StatusBadgeClass => IsEnabled ? "bg-success" : "bg-secondary";
}

/// <summary>
/// View model for feature flag audit log entries.
/// </summary>
public sealed record FeatureFlagAuditLogViewModel(
    Guid Id,
    Guid FeatureFlagId,
    string FeatureFlagKey,
    FeatureFlagAuditAction Action,
    Guid PerformedByUserId,
    UserRole PerformedByUserRole,
    string? PreviousValue,
    string? NewValue,
    string? Environment,
    string? Details,
    string? IpAddress,
    DateTime OccurredAt)
{
    /// <summary>
    /// Gets a display-friendly action string.
    /// </summary>
    public string ActionDisplay => Action switch
    {
        FeatureFlagAuditAction.Created => "Created",
        FeatureFlagAuditAction.Enabled => "Enabled",
        FeatureFlagAuditAction.Disabled => "Disabled",
        FeatureFlagAuditAction.TargetingUpdated => "Targeting Updated",
        FeatureFlagAuditAction.RolloutPercentageChanged => "Rollout Changed",
        FeatureFlagAuditAction.EnvironmentSettingsChanged => "Environment Changed",
        FeatureFlagAuditAction.MetadataUpdated => "Metadata Updated",
        FeatureFlagAuditAction.Deleted => "Deleted",
        FeatureFlagAuditAction.GlobalOverrideEnabled => "Global Override Enabled",
        FeatureFlagAuditAction.GlobalOverrideDisabled => "Global Override Disabled",
        _ => Action.ToString()
    };

    /// <summary>
    /// Gets a CSS class for the action badge.
    /// </summary>
    public string ActionBadgeClass => Action switch
    {
        FeatureFlagAuditAction.Created => "bg-success",
        FeatureFlagAuditAction.Enabled => "bg-success",
        FeatureFlagAuditAction.Disabled => "bg-secondary",
        FeatureFlagAuditAction.Deleted => "bg-danger",
        FeatureFlagAuditAction.GlobalOverrideEnabled => "bg-warning text-dark",
        FeatureFlagAuditAction.GlobalOverrideDisabled => "bg-info",
        _ => "bg-info"
    };
}
