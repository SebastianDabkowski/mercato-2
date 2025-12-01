using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Service for logging access to sensitive data and critical actions for audit and compliance purposes.
/// Failures are logged at critical level to enable monitoring and alerting.
/// </summary>
public sealed class AuditLoggingService : IAuditLoggingService
{
    private readonly ILogger<AuditLoggingService> _logger;
    private readonly ISensitiveAccessAuditLogRepository _sensitiveAccessAuditLogRepository;
    private readonly ICriticalActionAuditLogRepository _criticalActionAuditLogRepository;

    public AuditLoggingService(
        ILogger<AuditLoggingService> logger,
        ISensitiveAccessAuditLogRepository sensitiveAccessAuditLogRepository,
        ICriticalActionAuditLogRepository criticalActionAuditLogRepository)
    {
        _logger = logger;
        _sensitiveAccessAuditLogRepository = sensitiveAccessAuditLogRepository;
        _criticalActionAuditLogRepository = criticalActionAuditLogRepository;
    }

    /// <inheritdoc />
    public async Task LogSensitiveAccessAsync(
        Guid accessedByUserId,
        UserRole accessedByRole,
        SensitiveResourceType resourceType,
        Guid resourceId,
        SensitiveAccessAction action,
        Guid? resourceOwnerId = null,
        string? accessReason = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new SensitiveAccessAuditLog(
                accessedByUserId,
                accessedByRole,
                resourceType,
                resourceId,
                action,
                resourceOwnerId,
                accessReason,
                ipAddress,
                userAgent);

            await _sensitiveAccessAuditLogRepository.AddAsync(auditLog, cancellationToken);
            await _sensitiveAccessAuditLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: User {UserId} ({UserRole}) {Action} {ResourceType} {ResourceId}",
                accessedByUserId,
                accessedByRole,
                action,
                resourceType,
                resourceId);
        }
        catch (Exception ex)
        {
            // Log at critical level to enable monitoring and alerting systems to catch audit failures
            // This supports compliance requirements while not blocking the main operation
            _logger.LogCritical(ex,
                "AUDIT_FAILURE: Failed to create audit log for user {UserId} ({UserRole}) {Action} {ResourceType} {ResourceId}. " +
                "This indicates a potential compliance gap that requires immediate attention.",
                accessedByUserId,
                accessedByRole,
                action,
                resourceType,
                resourceId);

            // Also log the access attempt with available details for manual compliance review
            _logger.LogWarning(
                "AUDIT_FALLBACK: Sensitive access by user {UserId} ({UserRole}) to {ResourceType} {ResourceId} " +
                "at {Timestamp} from IP {IpAddress} could not be persisted to audit log.",
                accessedByUserId,
                accessedByRole,
                resourceType,
                resourceId,
                DateTime.UtcNow,
                ipAddress ?? "unknown");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetAuditLogsForResourceAsync(
        SensitiveResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await _sensitiveAccessAuditLogRepository.GetByResourceAsync(resourceType, resourceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetAuditLogsByAccessorAsync(
        Guid accessedByUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _sensitiveAccessAuditLogRepository.GetByAccessorAsync(accessedByUserId, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogCriticalActionAsync(
        Guid userId,
        UserRole userRole,
        CriticalActionType actionType,
        string targetResourceType,
        Guid? targetResourceId,
        CriticalActionOutcome outcome,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new CriticalActionAuditLog(
                userId,
                userRole,
                actionType,
                targetResourceType,
                targetResourceId,
                outcome,
                details,
                ipAddress,
                userAgent,
                correlationId);

            await _criticalActionAuditLogRepository.AddAsync(auditLog, cancellationToken);
            await _criticalActionAuditLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Critical action audit log created: User {UserId} ({UserRole}) performed {ActionType} on {ResourceType} {ResourceId} with outcome {Outcome}",
                userId,
                userRole,
                actionType,
                targetResourceType,
                targetResourceId,
                outcome);
        }
        catch (Exception ex)
        {
            // Log at critical level to enable monitoring and alerting systems to catch audit failures
            // This supports compliance requirements while not blocking the main operation
            _logger.LogCritical(ex,
                "CRITICAL_ACTION_AUDIT_FAILURE: Failed to create audit log for user {UserId} ({UserRole}) {ActionType} on {ResourceType} {ResourceId}. " +
                "This indicates a potential compliance gap that requires immediate attention.",
                userId,
                userRole,
                actionType,
                targetResourceType,
                targetResourceId);

            // Also log the action with available details for manual compliance review
            _logger.LogWarning(
                "CRITICAL_ACTION_AUDIT_FALLBACK: Critical action {ActionType} by user {UserId} ({UserRole}) on {ResourceType} {ResourceId} " +
                "at {Timestamp} from IP {IpAddress} with outcome {Outcome} could not be persisted to audit log. Details: {Details}",
                actionType,
                userId,
                userRole,
                targetResourceType,
                targetResourceId,
                DateTime.UtcNow,
                ipAddress ?? "unknown",
                outcome,
                details ?? "none");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CriticalActionAuditLog>> GetCriticalActionLogsByUserAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _criticalActionAuditLogRepository.GetByUserIdAsync(userId, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CriticalActionAuditLog>> GetCriticalActionLogsAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CriticalActionType? actionType = null,
        CriticalActionOutcome? outcome = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _criticalActionAuditLogRepository.GetByDateRangeAsync(
            fromDate,
            toDate,
            userId,
            actionType,
            outcome,
            skip,
            take,
            cancellationToken);
    }
}
