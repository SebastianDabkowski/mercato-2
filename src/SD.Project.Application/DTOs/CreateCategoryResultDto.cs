namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of a category creation operation.
/// </summary>
public sealed record CreateCategoryResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public CategoryDto? Category { get; init; }

    public static CreateCategoryResultDto Succeeded(CategoryDto category, string message = "Category created successfully.")
    {
        return new CreateCategoryResultDto
        {
            Success = true,
            Message = message,
            Category = category
        };
    }

    public static CreateCategoryResultDto Failed(string error)
    {
        return new CreateCategoryResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static CreateCategoryResultDto Failed(IReadOnlyList<string> errors)
    {
        return new CreateCategoryResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}
