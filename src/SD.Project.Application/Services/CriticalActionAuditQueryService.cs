using SD.Project.Application.DTOs;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Service for querying critical action audit logs with authorization enforcement.
/// Only Admin, Compliance, and Support roles can access audit logs.
/// </summary>
public sealed class CriticalActionAuditQueryService
{
    private readonly ICriticalActionAuditLogRepository _auditLogRepository;

    /// <summary>
    /// Roles that are authorized to view audit logs.
    /// </summary>
    private static readonly UserRole[] AuthorizedRoles = new[]
    {
        UserRole.Admin,
        UserRole.Compliance,
        UserRole.Support
    };

    public CriticalActionAuditQueryService(
        ICriticalActionAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    /// <summary>
    /// Queries critical action audit logs with the specified filters.
    /// Only Admin, Compliance, and Support roles are authorized to view audit logs.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user making the request.</param>
    /// <param name="requestingUserRole">The role of the user making the request.</param>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query result with audit logs, or null if unauthorized.</returns>
    public async Task<CriticalActionAuditLogQueryResultDto?> QueryAuditLogsAsync(
        Guid requestingUserId,
        UserRole requestingUserRole,
        CriticalActionAuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check - only privileged roles can view audit logs
        if (!IsAuthorizedToViewAuditLogs(requestingUserRole))
        {
            return null;
        }

        // Validate and normalize query parameters
        var take = Math.Min(Math.Max(query.Take, 1), 100);
        var skip = Math.Max(query.Skip, 0);

        // Get total count for pagination
        var totalCount = await _auditLogRepository.CountAsync(
            query.FromDate,
            query.ToDate,
            query.UserId,
            query.ActionType,
            query.Outcome,
            cancellationToken);

        // Get the audit logs
        var auditLogs = await _auditLogRepository.GetByDateRangeAsync(
            query.FromDate,
            query.ToDate,
            query.UserId,
            query.ActionType,
            query.Outcome,
            skip,
            take,
            cancellationToken);

        // Map to DTOs
        var items = auditLogs.Select(CriticalActionAuditLogDto.FromEntity).ToList();

        return new CriticalActionAuditLogQueryResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    /// <summary>
    /// Gets audit logs for a specific user.
    /// Only Admin, Compliance, and Support roles are authorized to view audit logs.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user making the request.</param>
    /// <param name="requestingUserRole">The role of the user making the request.</param>
    /// <param name="targetUserId">The ID of the user whose logs to retrieve.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log DTOs, or null if unauthorized.</returns>
    public async Task<IReadOnlyList<CriticalActionAuditLogDto>?> GetAuditLogsByUserAsync(
        Guid requestingUserId,
        UserRole requestingUserRole,
        Guid targetUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!IsAuthorizedToViewAuditLogs(requestingUserRole))
        {
            return null;
        }

        var auditLogs = await _auditLogRepository.GetByUserIdAsync(
            targetUserId,
            skip,
            Math.Min(take, 100),
            cancellationToken);

        return auditLogs.Select(CriticalActionAuditLogDto.FromEntity).ToList();
    }

    /// <summary>
    /// Gets audit logs for a specific action type.
    /// Only Admin, Compliance, and Support roles are authorized to view audit logs.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user making the request.</param>
    /// <param name="requestingUserRole">The role of the user making the request.</param>
    /// <param name="actionType">The action type to filter by.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log DTOs, or null if unauthorized.</returns>
    public async Task<IReadOnlyList<CriticalActionAuditLogDto>?> GetAuditLogsByActionTypeAsync(
        Guid requestingUserId,
        UserRole requestingUserRole,
        CriticalActionType actionType,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!IsAuthorizedToViewAuditLogs(requestingUserRole))
        {
            return null;
        }

        var auditLogs = await _auditLogRepository.GetByActionTypeAsync(
            actionType,
            skip,
            Math.Min(take, 100),
            cancellationToken);

        return auditLogs.Select(CriticalActionAuditLogDto.FromEntity).ToList();
    }

    /// <summary>
    /// Gets audit logs for a specific target resource.
    /// Only Admin, Compliance, and Support roles are authorized to view audit logs.
    /// </summary>
    /// <param name="requestingUserId">The ID of the user making the request.</param>
    /// <param name="requestingUserRole">The role of the user making the request.</param>
    /// <param name="targetResourceType">The type of resource.</param>
    /// <param name="targetResourceId">The ID of the resource.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log DTOs, or null if unauthorized.</returns>
    public async Task<IReadOnlyList<CriticalActionAuditLogDto>?> GetAuditLogsByResourceAsync(
        Guid requestingUserId,
        UserRole requestingUserRole,
        string targetResourceType,
        Guid targetResourceId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!IsAuthorizedToViewAuditLogs(requestingUserRole))
        {
            return null;
        }

        var auditLogs = await _auditLogRepository.GetByTargetResourceAsync(
            targetResourceType,
            targetResourceId,
            skip,
            Math.Min(take, 100),
            cancellationToken);

        return auditLogs.Select(CriticalActionAuditLogDto.FromEntity).ToList();
    }

    /// <summary>
    /// Checks if the given role is authorized to view audit logs.
    /// </summary>
    private static bool IsAuthorizedToViewAuditLogs(UserRole role)
    {
        return AuthorizedRoles.Contains(role);
    }
}
