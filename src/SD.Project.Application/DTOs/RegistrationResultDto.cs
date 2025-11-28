namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a user registration attempt.
/// </summary>
public sealed record RegistrationResultDto
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public string? Message { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public static RegistrationResultDto Succeeded(Guid userId, string message) =>
        new() { Success = true, UserId = userId, Message = message };

    public static RegistrationResultDto Failed(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToArray() };

    public static RegistrationResultDto Failed(string error) =>
        new() { Success = false, Errors = new[] { error } };
}
