namespace SD.Project.Application.Commands;

/// <summary>
/// Command to accept an internal user invitation.
/// </summary>
public sealed record AcceptInternalUserInvitationCommand(
    string InvitationToken,
    Guid UserId);
