namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get the impact assessment for account deletion.
/// </summary>
/// <param name="UserId">The ID of the user to assess.</param>
public sealed record GetAccountDeletionImpactQuery(Guid UserId);

/// <summary>
/// Query to get the current pending deletion request for a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
public sealed record GetPendingAccountDeletionQuery(Guid UserId);

/// <summary>
/// Query to get all deletion requests for a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
public sealed record GetAccountDeletionRequestsQuery(Guid UserId);

/// <summary>
/// Query to get audit logs for account deletions.
/// Used by admins to audit deletion events.
/// </summary>
/// <param name="UserId">The user ID to filter by (optional).</param>
/// <param name="DeletionRequestId">The deletion request ID to filter by (optional).</param>
public sealed record GetAccountDeletionAuditLogsQuery(
    Guid? UserId = null,
    Guid? DeletionRequestId = null);
