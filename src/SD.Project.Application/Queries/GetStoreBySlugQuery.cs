namespace SD.Project.Application.Queries;

/// <summary>
/// Query for retrieving a store by its URL slug.
/// </summary>
/// <param name="Slug">The store's URL slug.</param>
/// <param name="PublicOnly">When true, only returns publicly visible stores (Active or LimitedActive).</param>
public sealed record GetStoreBySlugQuery(string Slug, bool PublicOnly = false);
