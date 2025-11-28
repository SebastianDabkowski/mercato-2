namespace SD.Project.Application.Commands;

/// <summary>
/// Command to request a password reset email.
/// </summary>
/// <param name="Email">The email address associated with the account.</param>
public sealed record ForgotPasswordCommand(string Email);
