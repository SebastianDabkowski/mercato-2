namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of a delete image operation.
/// </summary>
public sealed record DeleteProductImageResultDto
{
    public bool Success { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    private DeleteProductImageResultDto() { }

    public static DeleteProductImageResultDto Succeeded()
        => new() { Success = true };

    public static DeleteProductImageResultDto Failed(string error)
        => new() { Success = false, Errors = new[] { error } };
}
