using System.Text.Json;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing feature flags.
/// </summary>
public sealed class FeatureFlagService
{
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly IFeatureFlagRepository _repository;

    public FeatureFlagService(
        ILogger<FeatureFlagService> logger,
        IFeatureFlagRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    public async Task<CreateFeatureFlagResultDto> HandleAsync(
        CreateFeatureFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate key format
        var validationErrors = ValidateFeatureFlagKey(command.Key);
        if (validationErrors.Count > 0)
        {
            return CreateFeatureFlagResultDto.Failed(validationErrors);
        }

        // Check for duplicate key
        var existingFlag = await _repository.GetByKeyAsync(command.Key, cancellationToken);
        if (existingFlag is not null)
        {
            return CreateFeatureFlagResultDto.Failed($"A feature flag with key '{command.Key}' already exists.");
        }

        try
        {
            var featureFlag = new FeatureFlag(
                command.Key,
                command.Name,
                command.Description,
                command.CreatedByUserId);

            await _repository.AddAsync(featureFlag, cancellationToken);

            // Create default environment configurations
            var defaultEnvironments = new[] { "development", "test", "staging", "production" };
            foreach (var env in defaultEnvironments)
            {
                var envConfig = new FeatureFlagEnvironment(featureFlag.Id, env, false);
                await _repository.AddEnvironmentAsync(envConfig, cancellationToken);
            }

            // Create audit log
            var auditLog = new FeatureFlagAuditLog(
                featureFlag.Id,
                featureFlag.Key,
                FeatureFlagAuditAction.Created,
                command.CreatedByUserId,
                command.CreatedByUserRole,
                previousValue: null,
                newValue: SerializeForAudit(featureFlag),
                environment: null,
                details: $"Created feature flag '{featureFlag.Name}'",
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent);

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Feature flag created: {FeatureFlagKey} by user {UserId}",
                featureFlag.Key,
                command.CreatedByUserId);

            var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
            return CreateFeatureFlagResultDto.Succeeded(MapToDto(featureFlag, environments));
        }
        catch (ArgumentException ex)
        {
            return CreateFeatureFlagResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates a feature flag's metadata.
    /// </summary>
    public async Task<UpdateFeatureFlagResultDto> HandleAsync(
        UpdateFeatureFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return UpdateFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var previousValue = SerializeForAudit(featureFlag);

        try
        {
            featureFlag.UpdateName(command.Name, command.ModifiedByUserId);
            featureFlag.UpdateDescription(command.Description, command.ModifiedByUserId);

            _repository.Update(featureFlag);

            var auditLog = new FeatureFlagAuditLog(
                featureFlag.Id,
                featureFlag.Key,
                FeatureFlagAuditAction.MetadataUpdated,
                command.ModifiedByUserId,
                command.ModifiedByUserRole,
                previousValue: previousValue,
                newValue: SerializeForAudit(featureFlag),
                environment: null,
                details: $"Updated name to '{command.Name}'",
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent);

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Feature flag updated: {FeatureFlagKey} by user {UserId}",
                featureFlag.Key,
                command.ModifiedByUserId);

            var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
            return UpdateFeatureFlagResultDto.Succeeded(MapToDto(featureFlag, environments));
        }
        catch (ArgumentException ex)
        {
            return UpdateFeatureFlagResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates a feature flag's targeting rules.
    /// </summary>
    public async Task<UpdateFeatureFlagResultDto> HandleAsync(
        UpdateFeatureFlagTargetingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return UpdateFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var previousValue = SerializeForAudit(featureFlag);

        try
        {
            featureFlag.UpdateRolloutPercentage(command.RolloutPercentage, command.ModifiedByUserId);
            featureFlag.UpdateTargetUserGroups(command.TargetUserGroups, command.ModifiedByUserId);
            featureFlag.UpdateTargetUserIds(command.TargetUserIds, command.ModifiedByUserId);
            featureFlag.UpdateTargetSellerIds(command.TargetSellerIds, command.ModifiedByUserId);

            _repository.Update(featureFlag);

            var auditLog = new FeatureFlagAuditLog(
                featureFlag.Id,
                featureFlag.Key,
                FeatureFlagAuditAction.TargetingUpdated,
                command.ModifiedByUserId,
                command.ModifiedByUserRole,
                previousValue: previousValue,
                newValue: SerializeForAudit(featureFlag),
                environment: null,
                details: $"Targeting updated: {command.RolloutPercentage}% rollout",
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent);

            await _repository.AddAuditLogAsync(auditLog, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Feature flag targeting updated: {FeatureFlagKey} by user {UserId}",
                featureFlag.Key,
                command.ModifiedByUserId);

            var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
            return UpdateFeatureFlagResultDto.Succeeded(MapToDto(featureFlag, environments));
        }
        catch (ArgumentException ex)
        {
            return UpdateFeatureFlagResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Enables a feature flag.
    /// </summary>
    public async Task<ToggleFeatureFlagResultDto> HandleAsync(
        EnableFeatureFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return ToggleFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var previousStatus = featureFlag.Status;
        featureFlag.Enable(command.ModifiedByUserId);
        _repository.Update(featureFlag);

        var auditLog = new FeatureFlagAuditLog(
            featureFlag.Id,
            featureFlag.Key,
            FeatureFlagAuditAction.Enabled,
            command.ModifiedByUserId,
            command.ModifiedByUserRole,
            previousValue: previousStatus.ToString(),
            newValue: featureFlag.Status.ToString(),
            environment: null,
            details: "Feature flag enabled for all users",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag enabled: {FeatureFlagKey} by user {UserId}",
            featureFlag.Key,
            command.ModifiedByUserId);

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        return ToggleFeatureFlagResultDto.Succeeded(
            MapToDto(featureFlag, environments),
            $"Feature flag '{featureFlag.Name}' has been enabled.");
    }

    /// <summary>
    /// Disables a feature flag.
    /// </summary>
    public async Task<ToggleFeatureFlagResultDto> HandleAsync(
        DisableFeatureFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return ToggleFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var previousStatus = featureFlag.Status;
        featureFlag.Disable(command.ModifiedByUserId);
        _repository.Update(featureFlag);

        var auditLog = new FeatureFlagAuditLog(
            featureFlag.Id,
            featureFlag.Key,
            FeatureFlagAuditAction.Disabled,
            command.ModifiedByUserId,
            command.ModifiedByUserRole,
            previousValue: previousStatus.ToString(),
            newValue: featureFlag.Status.ToString(),
            environment: null,
            details: "Feature flag disabled for all users",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag disabled: {FeatureFlagKey} by user {UserId}",
            featureFlag.Key,
            command.ModifiedByUserId);

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        return ToggleFeatureFlagResultDto.Succeeded(
            MapToDto(featureFlag, environments),
            $"Feature flag '{featureFlag.Name}' has been disabled.");
    }

    /// <summary>
    /// Enables targeted rollout for a feature flag.
    /// </summary>
    public async Task<ToggleFeatureFlagResultDto> HandleAsync(
        EnableFeatureFlagTargetingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return ToggleFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var previousStatus = featureFlag.Status;
        featureFlag.EnableTargeting(command.ModifiedByUserId);
        _repository.Update(featureFlag);

        var auditLog = new FeatureFlagAuditLog(
            featureFlag.Id,
            featureFlag.Key,
            FeatureFlagAuditAction.TargetingUpdated,
            command.ModifiedByUserId,
            command.ModifiedByUserRole,
            previousValue: previousStatus.ToString(),
            newValue: featureFlag.Status.ToString(),
            environment: null,
            details: "Targeted rollout enabled",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag targeting enabled: {FeatureFlagKey} by user {UserId}",
            featureFlag.Key,
            command.ModifiedByUserId);

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        return ToggleFeatureFlagResultDto.Succeeded(
            MapToDto(featureFlag, environments),
            $"Targeted rollout enabled for '{featureFlag.Name}'.");
    }

    /// <summary>
    /// Sets global override for a feature flag.
    /// </summary>
    public async Task<ToggleFeatureFlagResultDto> HandleAsync(
        SetFeatureFlagGlobalOverrideCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return ToggleFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var previousValue = featureFlag.GlobalOverride;
        featureFlag.SetGlobalOverride(command.Enabled, command.ModifiedByUserId);
        _repository.Update(featureFlag);

        var action = command.Enabled
            ? FeatureFlagAuditAction.GlobalOverrideEnabled
            : FeatureFlagAuditAction.GlobalOverrideDisabled;

        var auditLog = new FeatureFlagAuditLog(
            featureFlag.Id,
            featureFlag.Key,
            action,
            command.ModifiedByUserId,
            command.ModifiedByUserRole,
            previousValue: previousValue.ToString(),
            newValue: command.Enabled.ToString(),
            environment: null,
            details: command.Enabled ? "Global override enabled (emergency rollout)" : "Global override disabled",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag global override {Action}: {FeatureFlagKey} by user {UserId}",
            command.Enabled ? "enabled" : "disabled",
            featureFlag.Key,
            command.ModifiedByUserId);

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        var message = command.Enabled
            ? $"Global override enabled for '{featureFlag.Name}'. Feature is now ON for all users in all environments."
            : $"Global override disabled for '{featureFlag.Name}'.";

        return ToggleFeatureFlagResultDto.Succeeded(MapToDto(featureFlag, environments), message);
    }

    /// <summary>
    /// Updates environment-specific settings for a feature flag.
    /// </summary>
    public async Task<UpdateFeatureFlagResultDto> HandleAsync(
        UpdateFeatureFlagEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return UpdateFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var envConfig = await _repository.GetEnvironmentAsync(
            command.FeatureFlagId,
            command.Environment,
            cancellationToken);

        if (envConfig is null)
        {
            // Create new environment configuration
            envConfig = new FeatureFlagEnvironment(command.FeatureFlagId, command.Environment, command.IsEnabled);
            envConfig.SetRolloutPercentageOverride(command.RolloutPercentageOverride, command.ModifiedByUserId);
            await _repository.AddEnvironmentAsync(envConfig, cancellationToken);
        }
        else
        {
            var previousEnabled = envConfig.IsEnabled;
            if (command.IsEnabled)
            {
                envConfig.Enable(command.ModifiedByUserId);
            }
            else
            {
                envConfig.Disable(command.ModifiedByUserId);
            }
            envConfig.SetRolloutPercentageOverride(command.RolloutPercentageOverride, command.ModifiedByUserId);
            _repository.UpdateEnvironment(envConfig);
        }

        var auditLog = new FeatureFlagAuditLog(
            featureFlag.Id,
            featureFlag.Key,
            FeatureFlagAuditAction.EnvironmentSettingsChanged,
            command.ModifiedByUserId,
            command.ModifiedByUserRole,
            previousValue: null,
            newValue: $"{{\"enabled\":{command.IsEnabled.ToString().ToLowerInvariant()},\"rolloutPercentageOverride\":{(command.RolloutPercentageOverride?.ToString() ?? "null")}}}",
            environment: command.Environment,
            details: $"Environment '{command.Environment}' settings updated",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag environment updated: {FeatureFlagKey}/{Environment} by user {UserId}",
            featureFlag.Key,
            command.Environment,
            command.ModifiedByUserId);

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        return UpdateFeatureFlagResultDto.Succeeded(MapToDto(featureFlag, environments));
    }

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    public async Task<DeleteFeatureFlagResultDto> HandleAsync(
        DeleteFeatureFlagCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var featureFlag = await _repository.GetByIdAsync(command.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return DeleteFeatureFlagResultDto.Failed("Feature flag not found.");
        }

        var flagKey = featureFlag.Key;
        var flagName = featureFlag.Name;

        // Create audit log before deletion
        var auditLog = new FeatureFlagAuditLog(
            featureFlag.Id,
            featureFlag.Key,
            FeatureFlagAuditAction.Deleted,
            command.DeletedByUserId,
            command.DeletedByUserRole,
            previousValue: SerializeForAudit(featureFlag),
            newValue: null,
            environment: null,
            details: $"Feature flag '{flagName}' deleted",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent);

        await _repository.AddAuditLogAsync(auditLog, cancellationToken);

        _repository.Delete(featureFlag);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag deleted: {FeatureFlagKey} by user {UserId}",
            flagKey,
            command.DeletedByUserId);

        return DeleteFeatureFlagResultDto.Succeeded();
    }

    /// <summary>
    /// Gets all feature flags with optional filtering.
    /// </summary>
    public async Task<PagedResultDto<FeatureFlagDto>> HandleAsync(
        GetFeatureFlagsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (flags, totalCount) = await _repository.GetAllPagedAsync(
            query.SearchTerm,
            query.Status,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var items = new List<FeatureFlagDto>();
        foreach (var flag in flags)
        {
            var environments = await _repository.GetEnvironmentsByFlagIdAsync(flag.Id, cancellationToken);
            items.Add(MapToDto(flag, environments));
        }

        return PagedResultDto<FeatureFlagDto>.Create(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Gets a feature flag by its ID.
    /// </summary>
    public async Task<FeatureFlagDto?> HandleAsync(
        GetFeatureFlagByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var featureFlag = await _repository.GetByIdAsync(query.FeatureFlagId, cancellationToken);
        if (featureFlag is null)
        {
            return null;
        }

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        return MapToDto(featureFlag, environments);
    }

    /// <summary>
    /// Gets a feature flag by its key.
    /// </summary>
    public async Task<FeatureFlagDto?> HandleAsync(
        GetFeatureFlagByKeyQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var featureFlag = await _repository.GetByKeyAsync(query.Key, cancellationToken);
        if (featureFlag is null)
        {
            return null;
        }

        var environments = await _repository.GetEnvironmentsByFlagIdAsync(featureFlag.Id, cancellationToken);
        return MapToDto(featureFlag, environments);
    }

    /// <summary>
    /// Gets audit logs for feature flags.
    /// </summary>
    public async Task<PagedResultDto<FeatureFlagAuditLogDto>> HandleAsync(
        GetFeatureFlagAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (logs, totalCount) = await _repository.GetAuditLogsAsync(
            query.FeatureFlagId,
            query.UserId,
            query.Action,
            query.FromDate,
            query.ToDate,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var items = logs.Select(MapToAuditLogDto).ToList();

        return PagedResultDto<FeatureFlagAuditLogDto>.Create(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    private static FeatureFlagDto MapToDto(
        FeatureFlag flag,
        IReadOnlyCollection<FeatureFlagEnvironment> environments)
    {
        return new FeatureFlagDto(
            flag.Id,
            flag.Key,
            flag.Name,
            flag.Description,
            flag.Status,
            flag.GlobalOverride,
            flag.RolloutPercentage,
            flag.TargetUserGroups,
            flag.TargetUserIds,
            flag.TargetSellerIds,
            flag.CreatedByUserId,
            flag.LastModifiedByUserId,
            flag.CreatedAt,
            flag.UpdatedAt,
            environments.Select(MapToEnvironmentDto).ToList().AsReadOnly());
    }

    private static FeatureFlagEnvironmentDto MapToEnvironmentDto(FeatureFlagEnvironment env)
    {
        return new FeatureFlagEnvironmentDto(
            env.Id,
            env.FeatureFlagId,
            env.Environment,
            env.IsEnabled,
            env.RolloutPercentageOverride,
            env.LastModifiedByUserId,
            env.CreatedAt,
            env.UpdatedAt);
    }

    private static FeatureFlagAuditLogDto MapToAuditLogDto(FeatureFlagAuditLog log)
    {
        return new FeatureFlagAuditLogDto(
            log.Id,
            log.FeatureFlagId,
            log.FeatureFlagKey,
            log.Action,
            log.PerformedByUserId,
            log.PerformedByUserRole,
            log.PreviousValue,
            log.NewValue,
            log.Environment,
            log.Details,
            log.IpAddress,
            log.OccurredAt);
    }

    private static IReadOnlyList<string> ValidateFeatureFlagKey(string key)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add("Feature flag key is required.");
            return errors;
        }

        if (key.Length < 3)
        {
            errors.Add("Feature flag key must be at least 3 characters long.");
        }

        if (key.Length > 100)
        {
            errors.Add("Feature flag key cannot exceed 100 characters.");
        }

        foreach (var c in key)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
            {
                errors.Add("Feature flag key can only contain letters, numbers, hyphens, and underscores.");
                break;
            }
        }

        return errors;
    }

    private static string SerializeForAudit(FeatureFlag flag)
    {
        return JsonSerializer.Serialize(new
        {
            flag.Key,
            flag.Name,
            flag.Description,
            Status = flag.Status.ToString(),
            flag.GlobalOverride,
            flag.RolloutPercentage,
            flag.TargetUserGroups,
            flag.TargetUserIds,
            flag.TargetSellerIds
        });
    }
}
