using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying a single product's details.
/// </summary>
public class ProductModel : PageModel
{
    private readonly ILogger<ProductModel> _logger;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly StoreService _storeService;

    /// <summary>
    /// The product being viewed.
    /// </summary>
    public ProductViewModel? Product { get; private set; }

    /// <summary>
    /// The category ID for navigation (if category exists in the system).
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// The store information for the seller link.
    /// </summary>
    public StoreViewModel? Store { get; private set; }

    public ProductModel(
        ILogger<ProductModel> logger,
        ProductService productService,
        CategoryService categoryService,
        StoreService storeService)
    {
        _logger = logger;
        _productService = productService;
        _categoryService = categoryService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery] Guid? id,
        CancellationToken cancellationToken = default)
    {
        if (!id.HasValue)
        {
            _logger.LogWarning("Product page accessed without product ID");
            return Page();
        }

        var productDto = await _productService.HandleAsync(
            new GetProductByIdQuery(id.Value),
            cancellationToken);

        if (productDto is null)
        {
            _logger.LogWarning("Product {ProductId} not found", id.Value);
            return Page();
        }

        // Only show active products to buyers
        if (!productDto.IsActive || productDto.Status != Domain.Entities.ProductStatus.Active)
        {
            _logger.LogWarning("Product {ProductId} is not active (Status: {Status}, IsActive: {IsActive})", 
                id.Value, productDto.Status, productDto.IsActive);
            return Page();
        }

        Product = MapToViewModel(productDto);
        _logger.LogDebug("Loaded product {ProductId}: {ProductName}", id.Value, Product.Name);

        // Load category ID for navigation link
        if (!string.IsNullOrEmpty(Product.Category))
        {
            await LoadCategoryIdAsync(Product.Category, cancellationToken);
        }

        // Load store info for seller link
        if (productDto.StoreId.HasValue)
        {
            await LoadStoreInfoAsync(productDto.StoreId.Value, cancellationToken);
        }

        return Page();
    }

    private async Task LoadCategoryIdAsync(string categoryName, CancellationToken cancellationToken)
    {
        var category = await _categoryService.HandleAsync(new GetCategoryByNameQuery(categoryName), cancellationToken);
        
        if (category is not null)
        {
            CategoryId = category.Id;
            _logger.LogDebug("Found category ID {CategoryId} for category '{CategoryName}'", CategoryId, categoryName);
        }
        else
        {
            _logger.LogDebug("No category found matching '{CategoryName}'", categoryName);
        }
    }

    private async Task LoadStoreInfoAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var storeDto = await _storeService.HandleAsync(new GetStoreByIdQuery(storeId), cancellationToken);
        
        if (storeDto is not null && storeDto.IsPubliclyVisible)
        {
            Store = new StoreViewModel(storeDto.Id, storeDto.Name, storeDto.Slug);
            _logger.LogDebug("Loaded store info for product: {StoreName} ({StoreSlug})", storeDto.Name, storeDto.Slug);
        }
        else
        {
            _logger.LogDebug("Store {StoreId} not found or not publicly visible", storeId);
        }
    }

    private static ProductViewModel MapToViewModel(ProductDto dto)
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
