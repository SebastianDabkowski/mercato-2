using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for searching products by keyword with filter support.
/// </summary>
public class SearchModel : PageModel
{
    private const string FilterSessionKey = "SearchFilters";
    private const string SortSessionKey = "SearchSort";

    private readonly ILogger<SearchModel> _logger;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly StoreService _storeService;

    /// <summary>
    /// The search term entered by the user.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by category name.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    /// <summary>
    /// Minimum price filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Store/seller filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Flag to clear all filters.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool ClearFilters { get; set; }

    /// <summary>
    /// Selected sort option.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ProductSortOption? SortBy { get; set; }

    /// <summary>
    /// Products matching the search term and filters.
    /// </summary>
    public IReadOnlyCollection<ProductViewModel> Products { get; private set; } = Array.Empty<ProductViewModel>();

    /// <summary>
    /// Root categories for navigation when no search term is provided.
    /// </summary>
    public IReadOnlyCollection<CategoryViewModel> RootCategories { get; private set; } = Array.Empty<CategoryViewModel>();

    /// <summary>
    /// All active categories for filter dropdown.
    /// </summary>
    public IReadOnlyCollection<CategoryViewModel> AllCategories { get; private set; } = Array.Empty<CategoryViewModel>();

    /// <summary>
    /// Available stores for filter dropdown.
    /// </summary>
    public IReadOnlyCollection<StoreViewModel> AvailableStores { get; private set; } = Array.Empty<StoreViewModel>();

    /// <summary>
    /// Current filter state for the view.
    /// </summary>
    public ProductFilterViewModel Filters { get; private set; } = new();

    /// <summary>
    /// Indicates if any filters are currently active.
    /// </summary>
    public bool HasActiveFilters => Filters.HasActiveFilters;

    public SearchModel(
        ILogger<SearchModel> logger,
        ProductService productService,
        CategoryService categoryService,
        StoreService storeService)
    {
        _logger = logger;
        _productService = productService;
        _categoryService = categoryService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        // Load filter options
        await LoadFilterOptionsAsync(cancellationToken);

        // Handle clear filters action
        if (ClearFilters)
        {
            ClearAllFilters();
            return RedirectToPage(new { q = SearchTerm });
        }

        // Restore filters and sort from session if not provided in request
        RestoreFiltersFromSession();
        RestoreSortFromSession();

        // Determine effective sort option - default to Relevance for search
        var effectiveSortBy = SortBy ?? ProductSortOption.Relevance;

        // Build filter object
        Filters = new ProductFilterViewModel
        {
            Category = CategoryFilter,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            StoreId = StoreId
        };

        // Save filters and sort to session for persistence
        SaveFiltersToSession();
        SaveSortToSession(effectiveSortBy);

        // Check if we have any search or filter criteria
        bool hasSearchOrFilters = !string.IsNullOrWhiteSpace(SearchTerm) || Filters.HasActiveFilters;

        if (hasSearchOrFilters)
        {
            // Sanitize and limit search term length
            var sanitizedTerm = SearchTerm?.Trim() ?? string.Empty;
            if (sanitizedTerm.Length > 200)
            {
                sanitizedTerm = sanitizedTerm[..200];
            }

            var filterCriteria = new ProductFilterCriteria(
                Category: Filters.Category,
                MinPrice: Filters.MinPrice,
                MaxPrice: Filters.MaxPrice,
                StoreId: Filters.StoreId);

            var productDtos = await _productService.HandleAsync(
                new FilterProductsQuery(
                    SearchTerm: string.IsNullOrWhiteSpace(sanitizedTerm) ? null : sanitizedTerm,
                    Filters: filterCriteria,
                    SortBy: effectiveSortBy),
                cancellationToken);

            Products = productDtos
                .Select(MapToProductViewModel)
                .ToArray();

            _logger.LogDebug("Search for '{SearchTerm}' with filters returned {Count} results", sanitizedTerm, Products.Count);
        }

        return Page();
    }

    private async Task LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        // Load categories
        var allCategories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery(), cancellationToken);
        AllCategories = allCategories
            .Select(MapToCategoryViewModel)
            .ToArray();
        RootCategories = AllCategories
            .Where(c => c.ParentId is null)
            .ToArray();

        // Load stores
        var stores = await _storeService.HandleAsync(new GetPublicStoresQuery(), cancellationToken);
        AvailableStores = stores
            .Select(s => new StoreViewModel(s.Id, s.Name, s.Slug))
            .ToArray();
    }

    private void RestoreFiltersFromSession()
    {
        // Only restore if no explicit filter parameters were provided
        if (CategoryFilter is null && MinPrice is null && MaxPrice is null && StoreId is null)
        {
            var sessionFilters = HttpContext.Session.GetString(FilterSessionKey);
            if (!string.IsNullOrEmpty(sessionFilters))
            {
                try
                {
                    var filters = System.Text.Json.JsonSerializer.Deserialize<ProductFilterViewModel>(sessionFilters);
                    if (filters is not null)
                    {
                        CategoryFilter = filters.Category;
                        MinPrice = filters.MinPrice;
                        MaxPrice = filters.MaxPrice;
                        StoreId = filters.StoreId;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Invalid session data, ignore
                    HttpContext.Session.Remove(FilterSessionKey);
                }
            }
        }
    }

    private void SaveFiltersToSession()
    {
        if (Filters.HasActiveFilters)
        {
            var filterJson = System.Text.Json.JsonSerializer.Serialize(Filters);
            HttpContext.Session.SetString(FilterSessionKey, filterJson);
        }
        else
        {
            HttpContext.Session.Remove(FilterSessionKey);
        }
    }

    private void RestoreSortFromSession()
    {
        // Only restore if no explicit sort parameter was provided
        if (SortBy is null)
        {
            var sessionSort = HttpContext.Session.GetString(SortSessionKey);
            if (!string.IsNullOrEmpty(sessionSort) && Enum.TryParse<ProductSortOption>(sessionSort, out var sortOption))
            {
                SortBy = sortOption;
            }
        }
    }

    private void SaveSortToSession(ProductSortOption sortOption)
    {
        HttpContext.Session.SetString(SortSessionKey, sortOption.ToString());
    }

    private void ClearAllFilters()
    {
        CategoryFilter = null;
        MinPrice = null;
        MaxPrice = null;
        StoreId = null;
        HttpContext.Session.Remove(FilterSessionKey);
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
