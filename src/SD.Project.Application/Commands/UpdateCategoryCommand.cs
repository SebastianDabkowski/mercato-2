namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to update an existing category.
/// </summary>
public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    Guid? ParentId,
    int DisplayOrder);
