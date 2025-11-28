namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to resend the verification email.
/// </summary>
public sealed record ResendVerificationEmailCommand(string Email);
