namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents a search suggestion item.
/// </summary>
/// <param name="Text">The suggestion text to display.</param>
/// <param name="Type">The type of suggestion (Product, Category, Query).</param>
/// <param name="Id">Optional identifier for navigation (e.g., product or category ID).</param>
/// <param name="Url">Optional URL to navigate to when the suggestion is clicked.</param>
public sealed record SearchSuggestionDto(
    string Text,
    SearchSuggestionType Type,
    Guid? Id = null,
    string? Url = null);

/// <summary>
/// Types of search suggestions.
/// </summary>
public enum SearchSuggestionType
{
    /// <summary>
    /// A matching product name.
    /// </summary>
    Product,

    /// <summary>
    /// A matching category name.
    /// </summary>
    Category,

    /// <summary>
    /// A popular or historical search query.
    /// </summary>
    Query
}
