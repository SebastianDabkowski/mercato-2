namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of a category update operation.
/// </summary>
public sealed record UpdateCategoryResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public CategoryDto? Category { get; init; }

    public static UpdateCategoryResultDto Succeeded(CategoryDto category, string message = "Category updated successfully.")
    {
        return new UpdateCategoryResultDto
        {
            Success = true,
            Message = message,
            Category = category
        };
    }

    public static UpdateCategoryResultDto Failed(string error)
    {
        return new UpdateCategoryResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static UpdateCategoryResultDto Failed(IReadOnlyList<string> errors)
    {
        return new UpdateCategoryResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}
