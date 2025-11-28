namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a password reset attempt.
/// </summary>
public sealed record ResetPasswordResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public bool TokenExpired { get; init; }
    public bool TokenAlreadyUsed { get; init; }
    public bool TokenInvalid { get; init; }

    public static ResetPasswordResultDto Succeeded(string message) =>
        new() { Success = true, Message = message };

    public static ResetPasswordResultDto Failed(string message) =>
        new() { Success = false, Message = message };

    public static ResetPasswordResultDto Expired(string message) =>
        new() { Success = false, Message = message, TokenExpired = true };

    public static ResetPasswordResultDto AlreadyUsed(string message) =>
        new() { Success = false, Message = message, TokenAlreadyUsed = true };

    public static ResetPasswordResultDto Invalid(string message) =>
        new() { Success = false, Message = message, TokenInvalid = true };
}
