using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a critical action audit log entry.
/// </summary>
public sealed record CriticalActionAuditLogDto
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The ID of the user who performed the action.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The role of the user at the time of the action.
    /// </summary>
    public string UserRole { get; init; } = default!;

    /// <summary>
    /// The type of critical action performed.
    /// </summary>
    public string ActionType { get; init; } = default!;

    /// <summary>
    /// The type of resource the action was performed on.
    /// </summary>
    public string TargetResourceType { get; init; } = default!;

    /// <summary>
    /// The unique identifier of the target resource (optional).
    /// </summary>
    public Guid? TargetResourceId { get; init; }

    /// <summary>
    /// The outcome of the action.
    /// </summary>
    public string Outcome { get; init; } = default!;

    /// <summary>
    /// Additional details about the action.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// The IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// The user agent string of the client.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Correlation ID for tracing related actions.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The UTC timestamp when the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Creates a DTO from a domain entity.
    /// </summary>
    public static CriticalActionAuditLogDto FromEntity(CriticalActionAuditLog entity)
    {
        return new CriticalActionAuditLogDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            UserRole = entity.UserRole.ToString(),
            ActionType = entity.ActionType.ToString(),
            TargetResourceType = entity.TargetResourceType,
            TargetResourceId = entity.TargetResourceId,
            Outcome = entity.Outcome.ToString(),
            Details = entity.Details,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            CorrelationId = entity.CorrelationId,
            OccurredAt = entity.OccurredAt
        };
    }
}

/// <summary>
/// Query parameters for filtering critical action audit logs.
/// </summary>
public sealed record CriticalActionAuditLogQueryDto
{
    /// <summary>
    /// Start of the date range (inclusive). Required.
    /// </summary>
    public DateTime FromDate { get; init; }

    /// <summary>
    /// End of the date range (inclusive). Required.
    /// </summary>
    public DateTime ToDate { get; init; }

    /// <summary>
    /// Optional filter by user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Optional filter by action type.
    /// </summary>
    public CriticalActionType? ActionType { get; init; }

    /// <summary>
    /// Optional filter by outcome.
    /// </summary>
    public CriticalActionOutcome? Outcome { get; init; }

    /// <summary>
    /// Number of records to skip for pagination (default 0).
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Number of records to take (default 50, max 100).
    /// </summary>
    public int Take { get; init; } = 50;
}

/// <summary>
/// Result of a critical action audit log query.
/// </summary>
public sealed record CriticalActionAuditLogQueryResultDto
{
    /// <summary>
    /// The audit log entries matching the query.
    /// </summary>
    public IReadOnlyList<CriticalActionAuditLogDto> Items { get; init; } = Array.Empty<CriticalActionAuditLogDto>();

    /// <summary>
    /// Total count of matching records (for pagination).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of records skipped.
    /// </summary>
    public int Skip { get; init; }

    /// <summary>
    /// Number of records requested.
    /// </summary>
    public int Take { get; init; }
}
