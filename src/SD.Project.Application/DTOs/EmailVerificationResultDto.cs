namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of an email verification attempt.
/// </summary>
public sealed record EmailVerificationResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public bool TokenExpired { get; init; }
    public bool TokenAlreadyUsed { get; init; }
    public bool RequiresKyc { get; init; }

    public static EmailVerificationResultDto Succeeded(string message, bool requiresKyc = false) =>
        new() { Success = true, Message = message, RequiresKyc = requiresKyc };

    public static EmailVerificationResultDto Failed(string message) =>
        new() { Success = false, Message = message };

    public static EmailVerificationResultDto Expired(string message) =>
        new() { Success = false, Message = message, TokenExpired = true };

    public static EmailVerificationResultDto AlreadyUsed(string message) =>
        new() { Success = false, Message = message, TokenAlreadyUsed = true };
}
