using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class EditProductModel : PageModel
    {
        private readonly ILogger<EditProductModel> _logger;
        private readonly ProductService _productService;
        private readonly ProductImageService _productImageService;
        private readonly StoreService _storeService;

        [BindProperty]
        public EditProductViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public IReadOnlyCollection<ProductImageViewModel> Images { get; private set; } = Array.Empty<ProductImageViewModel>();

        public EditProductModel(
            ILogger<EditProductModel> logger,
            ProductService productService,
            ProductImageService productImageService,
            StoreService storeService)
        {
            _logger = logger;
            _productService = productService;
            _productImageService = productImageService;
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

            // Check if product belongs to this store and is not archived
            if (product.Status == ProductStatus.Archived)
            {
                return RedirectToPage("/Seller/Products", new { success = "This product has been deleted and cannot be edited." });
            }

            // Note: We can't check store ownership here easily without adding StoreId to ProductDto
            // The service layer will handle the authorization check on POST

            Input = new EditProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Amount,
                Currency = product.Currency,
                Stock = product.Stock,
                Category = product.Category,
                WeightKg = product.WeightKg,
                LengthCm = product.LengthCm,
                WidthCm = product.WidthCm,
                HeightCm = product.HeightCm
            };

            await LoadImagesAsync(id);

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

            if (!ModelState.IsValid)
            {
                await LoadImagesAsync(id);
                return Page();
            }

            var command = new UpdateProductCommand(
                id,
                userId,
                Input.Name.Trim(),
                Input.Description?.Trim(),
                Input.Price,
                Input.Currency.Trim().ToUpperInvariant(),
                Input.Stock,
                Input.Category.Trim(),
                Input.WeightKg,
                Input.LengthCm,
                Input.WidthCm,
                Input.HeightCm);

            var result = await _productService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Seller {UserId} updated product {ProductId}",
                    userId,
                    id);

                return RedirectToPage("/Seller/Products", new { success = "Product updated successfully." });
            }

            Errors = result.Errors;
            await LoadImagesAsync(id);
            return Page();
        }

        private async Task LoadImagesAsync(Guid productId)
        {
            var images = await _productImageService.HandleAsync(new GetProductImagesQuery(productId));
            Images = images.ToViewModels();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
