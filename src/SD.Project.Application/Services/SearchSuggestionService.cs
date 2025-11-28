using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for search suggestions.
/// </summary>
public sealed class SearchSuggestionService
{
    private const int MinimumSearchLength = 2;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public SearchSuggestionService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    /// <summary>
    /// Gets search suggestions based on partial user input.
    /// </summary>
    public async Task<IReadOnlyCollection<SearchSuggestionDto>> HandleAsync(
        GetSearchSuggestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var searchTerm = query.SearchTerm?.Trim();
        if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < MinimumSearchLength)
        {
            return Array.Empty<SearchSuggestionDto>();
        }

        var suggestions = new List<SearchSuggestionDto>();
        var maxResults = Math.Max(1, Math.Min(query.MaxResults, 10));

        // Get category suggestions (prioritize categories)
        var categorySuggestions = await _categoryRepository.GetSuggestionsAsync(
            searchTerm,
            maxResults,
            cancellationToken);

        foreach (var category in categorySuggestions)
        {
            suggestions.Add(new SearchSuggestionDto(
                category.Name,
                SearchSuggestionType.Category,
                category.Id,
                $"/Buyer/Category/{category.Id}"));
        }

        // Get product suggestions for remaining slots
        var remainingSlots = maxResults - suggestions.Count;
        if (remainingSlots > 0)
        {
            var productSuggestions = await _productRepository.GetProductSuggestionsAsync(
                searchTerm,
                remainingSlots,
                cancellationToken);

            foreach (var productName in productSuggestions)
            {
                // Only add if not already a category
                if (!suggestions.Any(s => s.Text.Equals(productName, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestions.Add(new SearchSuggestionDto(
                        productName,
                        SearchSuggestionType.Product,
                        null,
                        $"/Buyer/Search?q={Uri.EscapeDataString(productName)}"));
                }
            }
        }

        return suggestions.AsReadOnly();
    }
}
