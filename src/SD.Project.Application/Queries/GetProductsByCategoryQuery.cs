namespace SD.Project.Application.Queries;

/// <summary>
/// Query used to request products for a specific category.
/// </summary>
public sealed record GetProductsByCategoryQuery(string CategoryName);
