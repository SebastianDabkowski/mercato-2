namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of a category deletion operation.
/// </summary>
public sealed record DeleteCategoryResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static DeleteCategoryResultDto Succeeded(string message = "Category deleted successfully.")
    {
        return new DeleteCategoryResultDto
        {
            Success = true,
            Message = message
        };
    }

    public static DeleteCategoryResultDto Failed(string error)
    {
        return new DeleteCategoryResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static DeleteCategoryResultDto Failed(IReadOnlyList<string> errors)
    {
        return new DeleteCategoryResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}
