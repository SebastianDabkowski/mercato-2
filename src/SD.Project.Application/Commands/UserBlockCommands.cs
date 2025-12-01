using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to block a user account.
/// </summary>
public sealed record BlockUserCommand(
    Guid UserId,
    Guid AdminId,
    BlockReason Reason,
    string? Notes);

/// <summary>
/// Command to unblock a user account.
/// </summary>
public sealed record UnblockUserCommand(
    Guid UserId,
    Guid AdminId);
