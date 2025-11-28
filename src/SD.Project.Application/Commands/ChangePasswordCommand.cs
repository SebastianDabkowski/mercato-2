namespace SD.Project.Application.Commands;

/// <summary>
/// Command to change password for an authenticated user.
/// </summary>
/// <param name="UserId">The ID of the authenticated user.</param>
/// <param name="CurrentPassword">The user's current password.</param>
/// <param name="NewPassword">The new password to set.</param>
public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword);
