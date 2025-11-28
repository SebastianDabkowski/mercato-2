namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get search suggestions based on partial user input.
/// </summary>
/// <param name="SearchTerm">The partial search term to get suggestions for.</param>
/// <param name="MaxResults">Maximum number of suggestions to return.</param>
public sealed record GetSearchSuggestionsQuery(string SearchTerm, int MaxResults = 5);
