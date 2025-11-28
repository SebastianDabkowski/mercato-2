namespace SD.Project.Application.Queries;

/// <summary>
/// Query to search products by keyword across title and description.
/// </summary>
/// <param name="SearchTerm">The search term to match against product name and description.</param>
public sealed record SearchProductsQuery(string SearchTerm);
