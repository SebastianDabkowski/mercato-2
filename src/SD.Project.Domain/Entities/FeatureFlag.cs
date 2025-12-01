namespace SD.Project.Domain.Entities;

/// <summary>
/// The status of a feature flag.
/// </summary>
public enum FeatureFlagStatus
{
    /// <summary>Feature is disabled for all users.</summary>
    Disabled,
    /// <summary>Feature is enabled for all users.</summary>
    Enabled,
    /// <summary>Feature is enabled based on targeting rules.</summary>
    Targeted
}

/// <summary>
/// Represents a feature flag aggregate root.
/// Feature flags allow enabling, disabling or gradually rolling out features
/// without requiring code deployment.
/// </summary>
public class FeatureFlag
{
    /// <summary>
    /// Unique identifier for this feature flag.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique key used to reference the flag in code.
    /// Must be alphanumeric with hyphens/underscores, e.g., "new-checkout-flow".
    /// </summary>
    public string Key { get; private set; } = default!;

    /// <summary>
    /// Human-readable name for the feature flag.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Description of what this feature flag controls.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the feature flag.
    /// </summary>
    public FeatureFlagStatus Status { get; private set; }

    /// <summary>
    /// When true, the flag is globally enabled regardless of environment settings.
    /// Used for emergency rollouts.
    /// </summary>
    public bool GlobalOverride { get; private set; }

    /// <summary>
    /// The percentage of users who should see this feature (0-100).
    /// Only applies when Status is Targeted.
    /// </summary>
    public int RolloutPercentage { get; private set; }

    /// <summary>
    /// Comma-separated list of user group identifiers that should see this feature.
    /// e.g., "internal-users,beta-testers,sellers-premium"
    /// </summary>
    public string? TargetUserGroups { get; private set; }

    /// <summary>
    /// Comma-separated list of specific user IDs that should see this feature.
    /// </summary>
    public string? TargetUserIds { get; private set; }

    /// <summary>
    /// Comma-separated list of specific seller IDs that should see this feature.
    /// </summary>
    public string? TargetSellerIds { get; private set; }

    /// <summary>
    /// The ID of the user who created this feature flag.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// The ID of the user who last modified this feature flag.
    /// </summary>
    public Guid? LastModifiedByUserId { get; private set; }

    /// <summary>
    /// When the feature flag was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the feature flag was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private FeatureFlag()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    /// <param name="key">Unique key for the flag.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="description">Description of the feature.</param>
    /// <param name="createdByUserId">The user creating this flag.</param>
    public FeatureFlag(string key, string name, string description, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Feature flag key is required.", nameof(key));
        }

        if (!IsValidKey(key))
        {
            throw new ArgumentException(
                "Feature flag key must contain only alphanumeric characters, hyphens, and underscores.",
                nameof(key));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Feature flag name is required.", nameof(name));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user ID is required.", nameof(createdByUserId));
        }

        Id = Guid.NewGuid();
        Key = key.Trim().ToLowerInvariant();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = FeatureFlagStatus.Disabled;
        GlobalOverride = false;
        RolloutPercentage = 0;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates that a key contains only allowed characters.
    /// </summary>
    private static bool IsValidKey(string key)
    {
        foreach (var c in key)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Enables the feature flag for all users.
    /// </summary>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void Enable(Guid modifiedByUserId)
    {
        Status = FeatureFlagStatus.Enabled;
        GlobalOverride = false;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the feature flag for all users.
    /// </summary>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void Disable(Guid modifiedByUserId)
    {
        Status = FeatureFlagStatus.Disabled;
        GlobalOverride = false;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables targeted rollout for this flag.
    /// </summary>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void EnableTargeting(Guid modifiedByUserId)
    {
        Status = FeatureFlagStatus.Targeted;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the global override flag for emergency rollouts.
    /// When enabled, the flag is ON for all users regardless of environment settings.
    /// </summary>
    /// <param name="enabled">Whether to enable the global override.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void SetGlobalOverride(bool enabled, Guid modifiedByUserId)
    {
        GlobalOverride = enabled;
        if (enabled)
        {
            Status = FeatureFlagStatus.Enabled;
        }
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rollout percentage for targeted rollout.
    /// </summary>
    /// <param name="percentage">The percentage (0-100).</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void UpdateRolloutPercentage(int percentage, Guid modifiedByUserId)
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new ArgumentException("Rollout percentage must be between 0 and 100.", nameof(percentage));
        }

        RolloutPercentage = percentage;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the target user groups.
    /// </summary>
    /// <param name="userGroups">Comma-separated list of user group identifiers.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void UpdateTargetUserGroups(string? userGroups, Guid modifiedByUserId)
    {
        TargetUserGroups = userGroups?.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the target user IDs.
    /// </summary>
    /// <param name="userIds">Comma-separated list of user IDs.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void UpdateTargetUserIds(string? userIds, Guid modifiedByUserId)
    {
        TargetUserIds = userIds?.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the target seller IDs.
    /// </summary>
    /// <param name="sellerIds">Comma-separated list of seller IDs.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void UpdateTargetSellerIds(string? sellerIds, Guid modifiedByUserId)
    {
        TargetSellerIds = sellerIds?.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the name of the feature flag.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void UpdateName(string name, Guid modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Feature flag name is required.", nameof(name));
        }

        Name = name.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the description of the feature flag.
    /// </summary>
    /// <param name="description">The new description.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void UpdateDescription(string? description, Guid modifiedByUserId)
    {
        Description = description?.Trim() ?? string.Empty;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the parsed list of target user group identifiers.
    /// </summary>
    public IReadOnlyList<string> GetTargetUserGroupsList()
    {
        if (string.IsNullOrWhiteSpace(TargetUserGroups))
        {
            return Array.Empty<string>();
        }

        return TargetUserGroups
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(g => g.Trim().ToLowerInvariant())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the parsed list of target user IDs.
    /// </summary>
    public IReadOnlyList<Guid> GetTargetUserIdsList()
    {
        if (string.IsNullOrWhiteSpace(TargetUserIds))
        {
            return Array.Empty<Guid>();
        }

        var result = new List<Guid>();
        foreach (var id in TargetUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(id.Trim(), out var guid))
            {
                result.Add(guid);
            }
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// Gets the parsed list of target seller IDs.
    /// </summary>
    public IReadOnlyList<Guid> GetTargetSellerIdsList()
    {
        if (string.IsNullOrWhiteSpace(TargetSellerIds))
        {
            return Array.Empty<Guid>();
        }

        var result = new List<Guid>();
        foreach (var id in TargetSellerIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(id.Trim(), out var guid))
            {
                result.Add(guid);
            }
        }
        return result.AsReadOnly();
    }
}
