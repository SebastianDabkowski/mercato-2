namespace SD.Project.Application.DTOs;

/// <summary>
/// Result DTO for onboarding step operations.
/// </summary>
public sealed class OnboardingStepResultDto
{
    public bool Success { get; private set; }
    public string? Message { get; private set; }
    public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();

    private OnboardingStepResultDto() { }

    public static OnboardingStepResultDto Succeeded(string? message = null)
        => new() { Success = true, Message = message };

    public static OnboardingStepResultDto Failed(IEnumerable<string> errors)
        => new() { Success = false, Errors = errors.ToArray() };

    public static OnboardingStepResultDto Failed(string error)
        => new() { Success = false, Errors = new[] { error } };
}
