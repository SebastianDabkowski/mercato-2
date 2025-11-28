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

    /// <summary>
    /// The product being viewed.
    /// </summary>
    public ProductViewModel? Product { get; private set; }

    public ProductModel(
        ILogger<ProductModel> logger,
        ProductService productService)
    {
        _logger = logger;
        _productService = productService;
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

        return Page();
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
