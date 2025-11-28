using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for browsing products by category.
/// </summary>
public class CategoryModel : PageModel
{
    private readonly ILogger<CategoryModel> _logger;
    private readonly CategoryService _categoryService;
    private readonly ProductService _productService;

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

    public CategoryModel(
        ILogger<CategoryModel> logger,
        CategoryService categoryService,
        ProductService productService)
    {
        _logger = logger;
        _categoryService = categoryService;
        _productService = productService;
    }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        // Load all active categories
        var allCategories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery(), cancellationToken);

        // Get root categories (those without parent)
        RootCategories = allCategories
            .Where(c => c.ParentId is null)
            .Select(MapToViewModel)
            .ToArray();

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

            // Load products for this category
            var productDtos = await _productService.HandleAsync(new GetProductsByCategoryQuery(categoryDto.Name), cancellationToken);
            Products = productDtos
                .Select(MapToProductViewModel)
                .ToArray();

            _logger.LogDebug("Loaded category {CategoryName} with {ProductCount} products and {SubcategoryCount} subcategories",
                categoryDto.Name, Products.Count, Subcategories.Count);
        }

        return Page();
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
