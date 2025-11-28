namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a password change attempt.
/// </summary>
public sealed record ChangePasswordResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyCollection<string>? ValidationErrors { get; init; }

    public static ChangePasswordResultDto Succeeded(string message) =>
        new() { Success = true, Message = message };

    public static ChangePasswordResultDto Failed(string message) =>
        new() { Success = false, Message = message };

    public static ChangePasswordResultDto ValidationFailed(IReadOnlyCollection<string> errors) =>
        new() { Success = false, Message = "Password does not meet requirements.", ValidationErrors = errors };
}
