namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the types of feature flag audit actions.
/// </summary>
public enum FeatureFlagAuditAction
{
    /// <summary>Feature flag created.</summary>
    Created,
    /// <summary>Feature flag enabled.</summary>
    Enabled,
    /// <summary>Feature flag disabled.</summary>
    Disabled,
    /// <summary>Feature flag targeting updated.</summary>
    TargetingUpdated,
    /// <summary>Feature flag rollout percentage changed.</summary>
    RolloutPercentageChanged,
    /// <summary>Feature flag environment settings changed.</summary>
    EnvironmentSettingsChanged,
    /// <summary>Feature flag metadata (name/description) updated.</summary>
    MetadataUpdated,
    /// <summary>Feature flag deleted.</summary>
    Deleted,
    /// <summary>Global override enabled.</summary>
    GlobalOverrideEnabled,
    /// <summary>Global override disabled.</summary>
    GlobalOverrideDisabled
}

/// <summary>
/// Audit log entry for feature flag changes.
/// Provides traceability for all flag modifications as required for compliance.
/// </summary>
public class FeatureFlagAuditLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the feature flag that was modified.
    /// </summary>
    public Guid FeatureFlagId { get; private set; }

    /// <summary>
    /// The key of the feature flag at the time of the action.
    /// </summary>
    public string FeatureFlagKey { get; private set; } = default!;

    /// <summary>
    /// The type of action performed.
    /// </summary>
    public FeatureFlagAuditAction Action { get; private set; }

    /// <summary>
    /// The ID of the user who performed the action.
    /// </summary>
    public Guid PerformedByUserId { get; private set; }

    /// <summary>
    /// The role of the user at the time of the action.
    /// </summary>
    public UserRole PerformedByUserRole { get; private set; }

    /// <summary>
    /// JSON representation of the previous state (for updates).
    /// </summary>
    public string? PreviousValue { get; private set; }

    /// <summary>
    /// JSON representation of the new state.
    /// </summary>
    public string? NewValue { get; private set; }

    /// <summary>
    /// The environment affected (if environment-specific change).
    /// </summary>
    public string? Environment { get; private set; }

    /// <summary>
    /// Additional details about the change.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// The IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// The user agent string of the client.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    private FeatureFlagAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new feature flag audit log entry.
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag.</param>
    /// <param name="featureFlagKey">The key of the feature flag.</param>
    /// <param name="action">The action performed.</param>
    /// <param name="performedByUserId">The user who performed the action.</param>
    /// <param name="performedByUserRole">The role of the user.</param>
    /// <param name="previousValue">The previous value (JSON).</param>
    /// <param name="newValue">The new value (JSON).</param>
    /// <param name="environment">The environment affected.</param>
    /// <param name="details">Additional details.</param>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="userAgent">The user agent.</param>
    public FeatureFlagAuditLog(
        Guid featureFlagId,
        string featureFlagKey,
        FeatureFlagAuditAction action,
        Guid performedByUserId,
        UserRole performedByUserRole,
        string? previousValue = null,
        string? newValue = null,
        string? environment = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (featureFlagId == Guid.Empty)
        {
            throw new ArgumentException("Feature flag ID is required.", nameof(featureFlagId));
        }

        if (string.IsNullOrWhiteSpace(featureFlagKey))
        {
            throw new ArgumentException("Feature flag key is required.", nameof(featureFlagKey));
        }

        if (performedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Performed by user ID is required.", nameof(performedByUserId));
        }

        Id = Guid.NewGuid();
        FeatureFlagId = featureFlagId;
        FeatureFlagKey = featureFlagKey;
        Action = action;
        PerformedByUserId = performedByUserId;
        PerformedByUserRole = performedByUserRole;
        PreviousValue = previousValue?.Length > 4000 ? previousValue[..4000] : previousValue;
        NewValue = newValue?.Length > 4000 ? newValue[..4000] : newValue;
        Environment = environment?.Length > 50 ? environment[..50] : environment;
        Details = details?.Length > 1000 ? details[..1000] : details;
        IpAddress = ipAddress?.Length > 45 ? ipAddress[..45] : ipAddress;
        UserAgent = userAgent?.Length > 512 ? userAgent[..512] : userAgent;
        OccurredAt = DateTime.UtcNow;
    }
}
