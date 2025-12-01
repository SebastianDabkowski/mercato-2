using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Service for logging access to sensitive data for audit and compliance purposes.
/// Failures are logged at critical level to enable monitoring and alerting.
/// </summary>
public sealed class AuditLoggingService : IAuditLoggingService
{
    private readonly ILogger<AuditLoggingService> _logger;
    private readonly ISensitiveAccessAuditLogRepository _auditLogRepository;

    public AuditLoggingService(
        ILogger<AuditLoggingService> logger,
        ISensitiveAccessAuditLogRepository auditLogRepository)
    {
        _logger = logger;
        _auditLogRepository = auditLogRepository;
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

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            await _auditLogRepository.SaveChangesAsync(cancellationToken);

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
        return await _auditLogRepository.GetByResourceAsync(resourceType, resourceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensitiveAccessAuditLog>> GetAuditLogsByAccessorAsync(
        Guid accessedByUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.GetByAccessorAsync(accessedByUserId, skip, take, cancellationToken);
    }
}
