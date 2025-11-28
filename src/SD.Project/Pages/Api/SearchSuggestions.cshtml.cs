using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for search suggestions.
/// </summary>
public class SearchSuggestionsModel : PageModel
{
    private readonly SearchSuggestionService _suggestionService;

    public SearchSuggestionsModel(SearchSuggestionService suggestionService)
    {
        _suggestionService = suggestionService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery(Name = "q")] string? query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new JsonResult(Array.Empty<object>());
        }

        var suggestions = await _suggestionService.HandleAsync(
            new GetSearchSuggestionsQuery(query),
            cancellationToken);

        return new JsonResult(suggestions);
    }
}
