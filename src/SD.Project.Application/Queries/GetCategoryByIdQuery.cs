namespace SD.Project.Application.Queries;

/// <summary>
/// Query used to request a single category by ID.
/// </summary>
public sealed record GetCategoryByIdQuery(Guid CategoryId);
