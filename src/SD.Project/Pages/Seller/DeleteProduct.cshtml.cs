using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class DeleteProductModel : PageModel
    {
        private readonly ILogger<DeleteProductModel> _logger;
        private readonly ProductService _productService;
        private readonly StoreService _storeService;

        public ProductViewModel? Product { get; private set; }
        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();

        public DeleteProductModel(
            ILogger<DeleteProductModel> logger,
            ProductService productService,
            StoreService storeService)
        {
            _logger = logger;
            _productService = productService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            var product = await _productService.HandleAsync(new GetProductByIdQuery(id));
            if (product is null)
            {
                return NotFound();
            }

            // Check if product is already archived
            if (product.Status == ProductStatus.Archived)
            {
                return RedirectToPage("/Seller/Products", new { success = "This product has already been deleted." });
            }

            Product = new ProductViewModel(
                product.Id,
                product.Name,
                product.Description,
                product.Amount,
                product.Currency,
                product.Stock,
                product.Category,
                product.Status,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt,
                product.WeightKg,
                product.LengthCm,
                product.WidthCm,
                product.HeightCm);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            var command = new DeleteProductCommand(id, userId);
            var result = await _productService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Seller {UserId} deleted product {ProductId}",
                    userId,
                    id);

                return RedirectToPage("/Seller/Products", new { success = "Product deleted successfully." });
            }

            // If we have errors, we need to reload the product for display
            var productDto = await _productService.HandleAsync(new GetProductByIdQuery(id));
            if (productDto is not null)
            {
                Product = new ProductViewModel(
                    productDto.Id,
                    productDto.Name,
                    productDto.Description,
                    productDto.Amount,
                    productDto.Currency,
                    productDto.Stock,
                    productDto.Category,
                    productDto.Status,
                    productDto.IsActive,
                    productDto.CreatedAt,
                    productDto.UpdatedAt,
                    productDto.WeightKg,
                    productDto.LengthCm,
                    productDto.WidthCm,
                    productDto.HeightCm);
            }

            Errors = result.Errors;
            return Page();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
