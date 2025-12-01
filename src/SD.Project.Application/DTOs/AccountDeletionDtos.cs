using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for an account deletion request.
/// </summary>
public sealed record AccountDeletionRequestDto(
    Guid Id,
    Guid UserId,
    AccountDeletionRequestStatus Status,
    DateTime RequestedAt,
    DateTime? ConfirmedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? BlockingReason);

/// <summary>
/// Impact assessment for account deletion.
/// Informs the user about what will be affected by deletion.
/// </summary>
public sealed record AccountDeletionImpactDto(
    bool CanDelete,
    IReadOnlyList<string> BlockingConditions,
    int OrderCount,
    int ReviewCount,
    int AddressCount,
    bool HasActiveStore,
    string? StoreName,
    IReadOnlyList<string> ImpactSummary);

/// <summary>
/// Result of requesting an account deletion.
/// </summary>
public sealed record AccountDeletionRequestResultDto(
    bool Success,
    AccountDeletionRequestDto? Request,
    AccountDeletionImpactDto? Impact,
    string? Message,
    IReadOnlyList<string> Errors)
{
    public static AccountDeletionRequestResultDto Succeeded(
        AccountDeletionRequestDto request,
        AccountDeletionImpactDto impact,
        string message) =>
        new(true, request, impact, message, Array.Empty<string>());

    public static AccountDeletionRequestResultDto Failed(string error) =>
        new(false, null, null, null, new[] { error });

    public static AccountDeletionRequestResultDto Blocked(
        AccountDeletionImpactDto impact,
        IReadOnlyList<string> blockingConditions) =>
        new(false, null, impact, "Account deletion is blocked due to unresolved conditions.", blockingConditions);
}

/// <summary>
/// Result of confirming an account deletion.
/// </summary>
public sealed record AccountDeletionConfirmResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors)
{
    public static AccountDeletionConfirmResultDto Succeeded(string message) =>
        new(true, message, Array.Empty<string>());

    public static AccountDeletionConfirmResultDto Failed(string error) =>
        new(false, null, new[] { error });
}

/// <summary>
/// Result of cancelling an account deletion request.
/// </summary>
public sealed record AccountDeletionCancelResultDto(
    bool Success,
    string? Message)
{
    public static AccountDeletionCancelResultDto Succeeded() =>
        new(true, "Account deletion request has been cancelled.");

    public static AccountDeletionCancelResultDto Failed(string message) =>
        new(false, message);
}

/// <summary>
/// Audit log entry for account deletion.
/// </summary>
public sealed record AccountDeletionAuditLogDto(
    Guid Id,
    Guid DeletionRequestId,
    Guid AffectedUserId,
    Guid TriggeredByUserId,
    UserRole TriggeredByRole,
    AccountDeletionAuditAction Action,
    string? Notes,
    DateTime OccurredAt);
