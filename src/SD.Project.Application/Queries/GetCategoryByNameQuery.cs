namespace SD.Project.Application.Queries;

/// <summary>
/// Query used to request a single category by name.
/// </summary>
public sealed record GetCategoryByNameQuery(string CategoryName);
