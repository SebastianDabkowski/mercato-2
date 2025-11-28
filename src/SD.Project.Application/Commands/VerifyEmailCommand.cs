namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to verify an email using a verification token.
/// </summary>
public sealed record VerifyEmailCommand(string Token);
