namespace SD.Project.Application.Commands;

/// <summary>
/// Command to deactivate an internal user.
/// </summary>
public sealed record DeactivateInternalUserCommand(
    Guid InternalUserId,
    Guid RequestedByUserId);
