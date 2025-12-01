using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Service for evaluating feature flags at runtime.
/// </summary>
public interface IFeatureFlagEvaluator
{
    /// <summary>
    /// Evaluates a feature flag for the given context and returns whether it is enabled.
    /// </summary>
    /// <param name="query">The evaluation query containing flag key and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result including whether the flag is enabled and the reason.</returns>
    Task<FeatureFlagEvaluationResultDto> EvaluateAsync(
        EvaluateFeatureFlagQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature flag is enabled for the given context.
    /// This is a convenience method that returns just the boolean result.
    /// </summary>
    /// <param name="key">The feature flag key.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="userId">Optional user ID for targeting.</param>
    /// <param name="sellerId">Optional seller ID for targeting.</param>
    /// <param name="userGroups">Optional user groups for targeting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the feature is enabled for the given context.</returns>
    Task<bool> IsEnabledAsync(
        string key,
        string environment,
        Guid? userId = null,
        Guid? sellerId = null,
        IReadOnlyList<string>? userGroups = null,
        CancellationToken cancellationToken = default);
}
