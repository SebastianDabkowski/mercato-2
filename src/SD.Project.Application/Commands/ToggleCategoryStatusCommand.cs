namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to toggle a category's active status.
/// </summary>
public sealed record ToggleCategoryStatusCommand(Guid CategoryId);
