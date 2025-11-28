namespace SD.Project.Application.Commands;

/// <summary>
/// Command to reset password using a valid reset token.
/// </summary>
/// <param name="Token">The password reset token from the email link.</param>
/// <param name="NewPassword">The new password to set.</param>
public sealed record ResetPasswordCommand(string Token, string NewPassword);
