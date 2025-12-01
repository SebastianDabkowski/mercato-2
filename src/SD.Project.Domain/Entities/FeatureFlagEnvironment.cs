namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the environment-specific configuration for a feature flag.
/// Allows different flag states per environment (dev/test/stage/prod).
/// </summary>
public class FeatureFlagEnvironment
{
    /// <summary>
    /// Unique identifier for this environment configuration.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the parent feature flag.
    /// </summary>
    public Guid FeatureFlagId { get; private set; }

    /// <summary>
    /// The environment name (e.g., "development", "test", "staging", "production").
    /// </summary>
    public string Environment { get; private set; } = default!;

    /// <summary>
    /// Whether the flag is enabled in this environment.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Override rollout percentage for this specific environment.
    /// If null, uses the parent flag's rollout percentage.
    /// </summary>
    public int? RolloutPercentageOverride { get; private set; }

    /// <summary>
    /// The ID of the user who last modified this environment configuration.
    /// </summary>
    public Guid? LastModifiedByUserId { get; private set; }

    /// <summary>
    /// When this environment configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this environment configuration was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private FeatureFlagEnvironment()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new environment configuration for a feature flag.
    /// </summary>
    /// <param name="featureFlagId">The parent feature flag ID.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="isEnabled">Whether the flag is enabled in this environment.</param>
    public FeatureFlagEnvironment(Guid featureFlagId, string environment, bool isEnabled = false)
    {
        if (featureFlagId == Guid.Empty)
        {
            throw new ArgumentException("Feature flag ID is required.", nameof(featureFlagId));
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment name is required.", nameof(environment));
        }

        Id = Guid.NewGuid();
        FeatureFlagId = featureFlagId;
        Environment = environment.Trim().ToLowerInvariant();
        IsEnabled = isEnabled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables the flag in this environment.
    /// </summary>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void Enable(Guid modifiedByUserId)
    {
        IsEnabled = true;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the flag in this environment.
    /// </summary>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void Disable(Guid modifiedByUserId)
    {
        IsEnabled = false;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the rollout percentage override for this environment.
    /// </summary>
    /// <param name="percentage">The percentage (0-100) or null to use parent's value.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    public void SetRolloutPercentageOverride(int? percentage, Guid modifiedByUserId)
    {
        if (percentage.HasValue && (percentage.Value < 0 || percentage.Value > 100))
        {
            throw new ArgumentException("Rollout percentage must be between 0 and 100.", nameof(percentage));
        }

        RolloutPercentageOverride = percentage;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
