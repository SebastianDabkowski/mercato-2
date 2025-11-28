namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a forgot password request.
/// </summary>
public sealed record ForgotPasswordResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    /// <summary>
    /// Creates a success result. Returns same message whether or not email exists for security.
    /// </summary>
    public static ForgotPasswordResultDto Succeeded() =>
        new() { Success = true, Message = "If an account exists with this email, you will receive a password reset link shortly." };

    /// <summary>
    /// Creates a failure result for validation errors (not related to email existence).
    /// </summary>
    public static ForgotPasswordResultDto Failed(string message) =>
        new() { Success = false, Message = message };
}
