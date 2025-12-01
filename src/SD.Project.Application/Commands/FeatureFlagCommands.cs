using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new feature flag.
/// </summary>
public record CreateFeatureFlagCommand(
    string Key,
    string Name,
    string Description,
    Guid CreatedByUserId,
    UserRole CreatedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to update a feature flag's metadata.
/// </summary>
public record UpdateFeatureFlagCommand(
    Guid FeatureFlagId,
    string Name,
    string Description,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to update a feature flag's targeting rules.
/// </summary>
public record UpdateFeatureFlagTargetingCommand(
    Guid FeatureFlagId,
    int RolloutPercentage,
    string? TargetUserGroups,
    string? TargetUserIds,
    string? TargetSellerIds,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to enable a feature flag.
/// </summary>
public record EnableFeatureFlagCommand(
    Guid FeatureFlagId,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to disable a feature flag.
/// </summary>
public record DisableFeatureFlagCommand(
    Guid FeatureFlagId,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to enable targeted rollout for a feature flag.
/// </summary>
public record EnableFeatureFlagTargetingCommand(
    Guid FeatureFlagId,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to set global override for a feature flag.
/// </summary>
public record SetFeatureFlagGlobalOverrideCommand(
    Guid FeatureFlagId,
    bool Enabled,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to update environment-specific settings for a feature flag.
/// </summary>
public record UpdateFeatureFlagEnvironmentCommand(
    Guid FeatureFlagId,
    string Environment,
    bool IsEnabled,
    int? RolloutPercentageOverride,
    Guid ModifiedByUserId,
    UserRole ModifiedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to delete a feature flag.
/// </summary>
public record DeleteFeatureFlagCommand(
    Guid FeatureFlagId,
    Guid DeletedByUserId,
    UserRole DeletedByUserRole,
    string? IpAddress = null,
    string? UserAgent = null);
