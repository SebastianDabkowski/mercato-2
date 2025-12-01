namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to create a new category.
/// </summary>
public sealed record CreateCategoryCommand(
    string Name,
    Guid? ParentId,
    int DisplayOrder = 0,
    string? Description = null,
    string? Slug = null);
