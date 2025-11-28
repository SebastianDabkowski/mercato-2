namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of deleting (archiving) a product.
/// </summary>
public sealed record DeleteProductResultDto(
    bool Success,
    IReadOnlyList<string> Errors)
{
    public static DeleteProductResultDto Succeeded()
        => new(true, Array.Empty<string>());

    public static DeleteProductResultDto Failed(string error)
        => new(false, new[] { error });

    public static DeleteProductResultDto Failed(IReadOnlyList<string> errors)
        => new(false, errors);
}
