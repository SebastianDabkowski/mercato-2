using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all feature flags with optional filtering.
/// </summary>
public record GetFeatureFlagsQuery(
    string? SearchTerm = null,
    FeatureFlagStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get a feature flag by its ID.
/// </summary>
public record GetFeatureFlagByIdQuery(Guid FeatureFlagId);

/// <summary>
/// Query to get a feature flag by its key.
/// </summary>
public record GetFeatureFlagByKeyQuery(string Key);

/// <summary>
/// Query to get audit logs for a feature flag.
/// </summary>
public record GetFeatureFlagAuditLogsQuery(
    Guid? FeatureFlagId = null,
    Guid? UserId = null,
    FeatureFlagAuditAction? Action = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50);

/// <summary>
/// Query to evaluate a feature flag for a specific context.
/// </summary>
public record EvaluateFeatureFlagQuery(
    string Key,
    string Environment,
    Guid? UserId = null,
    Guid? SellerId = null,
    IReadOnlyList<string>? UserGroups = null);
