using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for searching products by keyword.
/// </summary>
public class SearchModel : PageModel
{
    private readonly ILogger<SearchModel> _logger;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;

    /// <summary>
    /// The search term entered by the user.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Products matching the search term.
    /// </summary>
    public IReadOnlyCollection<ProductViewModel> Products { get; private set; } = Array.Empty<ProductViewModel>();

    /// <summary>
    /// Root categories for navigation when no search term is provided.
    /// </summary>
    public IReadOnlyCollection<CategoryViewModel> RootCategories { get; private set; } = Array.Empty<CategoryViewModel>();

    public SearchModel(
        ILogger<SearchModel> logger,
        ProductService productService,
        CategoryService categoryService)
    {
        _logger = logger;
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        // Load root categories for suggestions
        var allCategories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery(), cancellationToken);
        RootCategories = allCategories
            .Where(c => c.ParentId is null)
            .Select(MapToCategoryViewModel)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            // Sanitize and limit search term length
            var sanitizedTerm = SearchTerm.Trim();
            if (sanitizedTerm.Length > 200)
            {
                sanitizedTerm = sanitizedTerm.Substring(0, 200);
            }

            var productDtos = await _productService.HandleAsync(new SearchProductsQuery(sanitizedTerm), cancellationToken);
            Products = productDtos
                .Select(MapToProductViewModel)
                .ToArray();

            _logger.LogDebug("Search for '{SearchTerm}' returned {Count} results", sanitizedTerm, Products.Count);
        }

        return Page();
    }

    private static CategoryViewModel MapToCategoryViewModel(CategoryDto dto)
    {
        return new CategoryViewModel(
            dto.Id,
            dto.Name,
            dto.ParentId,
            dto.ParentName,
            dto.DisplayOrder,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.ProductCount,
            dto.ChildCount);
    }

    private static ProductViewModel MapToProductViewModel(ProductDto dto)
    {
        return new ProductViewModel(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.Amount,
            dto.Currency,
            dto.Stock,
            dto.Category,
            dto.Status,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.WeightKg,
            dto.LengthCm,
            dto.WidthCm,
            dto.HeightCm,
            dto.MainImageUrl,
            dto.MainImageThumbnailUrl);
    }
}
