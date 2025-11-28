namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to delete a category.
/// </summary>
public sealed record DeleteCategoryCommand(Guid CategoryId);
