using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for browsing products by category with filter support.
/// </summary>
public class CategoryModel : PageModel
{
    private const string FilterSessionKey = "CategoryFilters";
    private const string SortSessionKey = "CategorySort";

    private readonly ILogger<CategoryModel> _logger;
    private readonly CategoryService _categoryService;
    private readonly ProductService _productService;
    private readonly StoreService _storeService;

    /// <summary>
    /// The current category being viewed.
    /// </summary>
    public CategoryViewModel? CurrentCategory { get; private set; }

    /// <summary>
    /// Products in the current category.
    /// </summary>
    public IReadOnlyCollection<ProductViewModel> Products { get; private set; } = Array.Empty<ProductViewModel>();

    /// <summary>
    /// Subcategories of the current category (if any).
    /// </summary>
    public IReadOnlyCollection<CategoryViewModel> Subcategories { get; private set; } = Array.Empty<CategoryViewModel>();

    /// <summary>
    /// Root categories for navigation when no category is selected or for empty state suggestions.
    /// </summary>
    public IReadOnlyCollection<CategoryViewModel> RootCategories { get; private set; } = Array.Empty<CategoryViewModel>();

    /// <summary>
    /// Parent category for breadcrumb navigation (if current category has a parent).
    /// </summary>
    public CategoryViewModel? ParentCategory { get; private set; }

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

    public CategoryModel(
        ILogger<CategoryModel> logger,
        CategoryService categoryService,
        ProductService productService,
        StoreService storeService)
    {
        _logger = logger;
        _categoryService = categoryService;
        _productService = productService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        // Load filter options
        var stores = await _storeService.HandleAsync(new GetPublicStoresQuery(), cancellationToken);
        AvailableStores = stores
            .Select(s => new StoreViewModel(s.Id, s.Name, s.Slug))
            .ToArray();

        // Load all active categories
        var allCategories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery(), cancellationToken);

        // Get root categories (those without parent)
        RootCategories = allCategories
            .Where(c => c.ParentId is null)
            .Select(MapToViewModel)
            .ToArray();

        // Handle clear filters action
        if (ClearFilters)
        {
            ClearAllFilters();
            return RedirectToPage(new { id });
        }

        // Restore filters and sort from session if not provided in request
        RestoreFiltersFromSession();
        RestoreSortFromSession();

        // Determine effective sort option - default to Newest for category browsing
        var effectiveSortBy = SortBy ?? ProductSortOption.Newest;

        // Build filter object
        Filters = new ProductFilterViewModel
        {
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            StoreId = StoreId
        };

        // Save filters and sort to session for persistence
        SaveFiltersToSession();
        SaveSortToSession(effectiveSortBy);

        if (id.HasValue)
        {
            // Load specific category
            var categoryDto = await _categoryService.HandleAsync(new GetCategoryByIdQuery(id.Value), cancellationToken);

            if (categoryDto is null || !categoryDto.IsActive)
            {
                _logger.LogWarning("Category {CategoryId} not found or inactive", id.Value);
                return NotFound();
            }

            CurrentCategory = MapToViewModel(categoryDto);

            // Load parent category if exists
            if (categoryDto.ParentId.HasValue)
            {
                var parentDto = await _categoryService.HandleAsync(new GetCategoryByIdQuery(categoryDto.ParentId.Value), cancellationToken);
                if (parentDto is not null)
                {
                    ParentCategory = MapToViewModel(parentDto);
                }
            }

            // Load subcategories
            Subcategories = allCategories
                .Where(c => c.ParentId == id.Value)
                .Select(MapToViewModel)
                .ToArray();

            // Load products for this category with filters and sorting applied
            var filterCriteria = new ProductFilterCriteria(
                Category: categoryDto.Name,
                MinPrice: Filters.MinPrice,
                MaxPrice: Filters.MaxPrice,
                StoreId: Filters.StoreId);

            var productDtos = await _productService.HandleAsync(
                new FilterProductsQuery(Filters: filterCriteria, SortBy: effectiveSortBy),
                cancellationToken);

            Products = productDtos
                .Select(MapToProductViewModel)
                .ToArray();

            _logger.LogDebug("Loaded category {CategoryName} with {ProductCount} products and {SubcategoryCount} subcategories",
                categoryDto.Name, Products.Count, Subcategories.Count);
        }

        return Page();
    }

    private void RestoreFiltersFromSession()
    {
        // Only restore if no explicit filter parameters were provided
        if (MinPrice is null && MaxPrice is null && StoreId is null)
        {
            var sessionFilters = HttpContext.Session.GetString(FilterSessionKey);
            if (!string.IsNullOrEmpty(sessionFilters))
            {
                try
                {
                    var filters = System.Text.Json.JsonSerializer.Deserialize<ProductFilterViewModel>(sessionFilters);
                    if (filters is not null)
                    {
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
        MinPrice = null;
        MaxPrice = null;
        StoreId = null;
        HttpContext.Session.Remove(FilterSessionKey);
    }

    private static CategoryViewModel MapToViewModel(CategoryDto dto)
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
