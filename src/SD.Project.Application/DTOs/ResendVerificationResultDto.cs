namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a resend verification email attempt.
/// </summary>
public sealed record ResendVerificationResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static ResendVerificationResultDto Succeeded(string message) =>
        new() { Success = true, Message = message };

    public static ResendVerificationResultDto Failed(string message) =>
        new() { Success = false, Message = message };
}
