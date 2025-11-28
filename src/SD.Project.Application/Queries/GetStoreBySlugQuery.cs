namespace SD.Project.Application.Queries;

/// <summary>
/// Query for retrieving a store by its URL slug.
/// </summary>
public sealed record GetStoreBySlugQuery(string Slug);
