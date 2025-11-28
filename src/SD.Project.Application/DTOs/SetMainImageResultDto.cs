namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of a set main image operation.
/// </summary>
public sealed record SetMainImageResultDto
{
    public bool Success { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    private SetMainImageResultDto() { }

    public static SetMainImageResultDto Succeeded()
        => new() { Success = true };

    public static SetMainImageResultDto Failed(string error)
        => new() { Success = false, Errors = new[] { error } };
}
