using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for a feature flag.
/// </summary>
public record FeatureFlagDto(
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
    IReadOnlyCollection<FeatureFlagEnvironmentDto> Environments);

/// <summary>
/// Data transfer object for a feature flag environment configuration.
/// </summary>
public record FeatureFlagEnvironmentDto(
    Guid Id,
    Guid FeatureFlagId,
    string Environment,
    bool IsEnabled,
    int? RolloutPercentageOverride,
    Guid? LastModifiedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Data transfer object for a feature flag audit log entry.
/// </summary>
public record FeatureFlagAuditLogDto(
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
    DateTime OccurredAt);

/// <summary>
/// Result of creating a feature flag.
/// </summary>
public record CreateFeatureFlagResultDto
{
    public bool Success { get; init; }
    public FeatureFlagDto? FeatureFlag { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static CreateFeatureFlagResultDto Succeeded(FeatureFlagDto featureFlag) =>
        new() { Success = true, FeatureFlag = featureFlag };

    public static CreateFeatureFlagResultDto Failed(params string[] errors) =>
        new() { Success = false, Errors = errors };

    public static CreateFeatureFlagResultDto Failed(IReadOnlyList<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of updating a feature flag.
/// </summary>
public record UpdateFeatureFlagResultDto
{
    public bool Success { get; init; }
    public FeatureFlagDto? FeatureFlag { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static UpdateFeatureFlagResultDto Succeeded(FeatureFlagDto featureFlag) =>
        new() { Success = true, FeatureFlag = featureFlag };

    public static UpdateFeatureFlagResultDto Failed(params string[] errors) =>
        new() { Success = false, Errors = errors };

    public static UpdateFeatureFlagResultDto Failed(IReadOnlyList<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of toggling a feature flag.
/// </summary>
public record ToggleFeatureFlagResultDto
{
    public bool Success { get; init; }
    public FeatureFlagDto? FeatureFlag { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ToggleFeatureFlagResultDto Succeeded(FeatureFlagDto featureFlag, string message) =>
        new() { Success = true, FeatureFlag = featureFlag, Message = message };

    public static ToggleFeatureFlagResultDto Failed(params string[] errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of deleting a feature flag.
/// </summary>
public record DeleteFeatureFlagResultDto
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static DeleteFeatureFlagResultDto Succeeded() =>
        new() { Success = true };

    public static DeleteFeatureFlagResultDto Failed(params string[] errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of evaluating a feature flag for a request context.
/// </summary>
public record FeatureFlagEvaluationResultDto(
    string Key,
    bool IsEnabled,
    string Reason);
