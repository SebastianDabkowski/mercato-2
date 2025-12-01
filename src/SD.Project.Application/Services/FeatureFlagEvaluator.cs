using Microsoft.Extensions.Logging;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Evaluates feature flags at runtime based on context and targeting rules.
/// </summary>
public sealed class FeatureFlagEvaluator : IFeatureFlagEvaluator
{
    private readonly ILogger<FeatureFlagEvaluator> _logger;
    private readonly IFeatureFlagRepository _repository;

    public FeatureFlagEvaluator(
        ILogger<FeatureFlagEvaluator> logger,
        IFeatureFlagRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<FeatureFlagEvaluationResultDto> EvaluateAsync(
        EvaluateFeatureFlagQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Key))
        {
            return new FeatureFlagEvaluationResultDto(
                query.Key ?? string.Empty,
                false,
                "Invalid flag key");
        }

        var flag = await _repository.GetByKeyAsync(query.Key, cancellationToken);
        if (flag is null)
        {
            _logger.LogDebug("Feature flag not found: {Key}", query.Key);
            return new FeatureFlagEvaluationResultDto(
                query.Key,
                false,
                "Flag not found");
        }

        // Check global override first
        if (flag.GlobalOverride)
        {
            _logger.LogDebug(
                "Feature flag {Key} evaluated as enabled (global override)",
                query.Key);
            return new FeatureFlagEvaluationResultDto(
                query.Key,
                true,
                "Global override enabled");
        }

        // Check environment-specific settings
        var envConfig = await _repository.GetEnvironmentAsync(flag.Id, query.Environment, cancellationToken);
        if (envConfig is not null && !envConfig.IsEnabled)
        {
            _logger.LogDebug(
                "Feature flag {Key} evaluated as disabled (environment: {Environment})",
                query.Key,
                query.Environment);
            return new FeatureFlagEvaluationResultDto(
                query.Key,
                false,
                $"Disabled in environment: {query.Environment}");
        }

        // Check flag status
        switch (flag.Status)
        {
            case FeatureFlagStatus.Disabled:
                _logger.LogDebug("Feature flag {Key} evaluated as disabled (status)", query.Key);
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    false,
                    "Flag is disabled");

            case FeatureFlagStatus.Enabled:
                _logger.LogDebug("Feature flag {Key} evaluated as enabled (status)", query.Key);
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    true,
                    "Flag is enabled");

            case FeatureFlagStatus.Targeted:
                return EvaluateTargeting(flag, query, envConfig);

            default:
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    false,
                    "Unknown flag status");
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(
        string key,
        string environment,
        Guid? userId = null,
        Guid? sellerId = null,
        IReadOnlyList<string>? userGroups = null,
        CancellationToken cancellationToken = default)
    {
        var query = new EvaluateFeatureFlagQuery(key, environment, userId, sellerId, userGroups);
        var result = await EvaluateAsync(query, cancellationToken);
        return result.IsEnabled;
    }

    private FeatureFlagEvaluationResultDto EvaluateTargeting(
        FeatureFlag flag,
        EvaluateFeatureFlagQuery query,
        FeatureFlagEnvironment? envConfig)
    {
        // Check if user ID is in target list
        if (query.UserId.HasValue)
        {
            var targetUserIds = flag.GetTargetUserIdsList();
            if (targetUserIds.Contains(query.UserId.Value))
            {
                _logger.LogDebug(
                    "Feature flag {Key} evaluated as enabled (user in target list)",
                    query.Key);
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    true,
                    "User in target list");
            }
        }

        // Check if seller ID is in target list
        if (query.SellerId.HasValue)
        {
            var targetSellerIds = flag.GetTargetSellerIdsList();
            if (targetSellerIds.Contains(query.SellerId.Value))
            {
                _logger.LogDebug(
                    "Feature flag {Key} evaluated as enabled (seller in target list)",
                    query.Key);
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    true,
                    "Seller in target list");
            }
        }

        // Check if user is in a target group
        if (query.UserGroups is not null && query.UserGroups.Count > 0)
        {
            var targetGroups = flag.GetTargetUserGroupsList();
            var userGroupsLower = query.UserGroups.Select(g => g.ToLowerInvariant()).ToHashSet();
            if (targetGroups.Any(g => userGroupsLower.Contains(g)))
            {
                _logger.LogDebug(
                    "Feature flag {Key} evaluated as enabled (user in target group)",
                    query.Key);
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    true,
                    "User in target group");
            }
        }

        // Check rollout percentage
        var rolloutPercentage = envConfig?.RolloutPercentageOverride ?? flag.RolloutPercentage;
        if (rolloutPercentage > 0 && query.UserId.HasValue)
        {
            // Use consistent hashing based on user ID and flag key to ensure
            // users get consistent flag values across requests
            var hash = GetConsistentHash(query.UserId.Value, flag.Key);
            var bucket = hash % 100;

            if (bucket < rolloutPercentage)
            {
                _logger.LogDebug(
                    "Feature flag {Key} evaluated as enabled (user {UserId} in rollout, bucket {Bucket} < {Percentage}%)",
                    query.Key,
                    query.UserId,
                    bucket,
                    rolloutPercentage);
                return new FeatureFlagEvaluationResultDto(
                    query.Key,
                    true,
                    $"User in {rolloutPercentage}% rollout");
            }
        }

        _logger.LogDebug(
            "Feature flag {Key} evaluated as disabled (no targeting rules matched)",
            query.Key);
        return new FeatureFlagEvaluationResultDto(
            query.Key,
            false,
            "No targeting rules matched");
    }

    /// <summary>
    /// Generates a consistent hash for a user and flag combination.
    /// This ensures users get the same result for a flag across requests.
    /// </summary>
    private static int GetConsistentHash(Guid userId, string flagKey)
    {
        var combined = $"{userId}:{flagKey}";
        unchecked
        {
            int hash = 17;
            foreach (var c in combined)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash);
        }
    }
}
