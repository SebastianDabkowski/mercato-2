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
    public class ProductImagesModel : PageModel
    {
        private readonly ILogger<ProductImagesModel> _logger;
        private readonly ProductService _productService;
        private readonly ProductImageService _productImageService;
        private readonly StoreService _storeService;

        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; } = string.Empty;
        public IReadOnlyCollection<ProductImageViewModel> Images { get; private set; } = Array.Empty<ProductImageViewModel>();
        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }

        public ProductImagesModel(
            ILogger<ProductImagesModel> logger,
            ProductService productService,
            ProductImageService productImageService,
            StoreService storeService)
        {
            _logger = logger;
            _productService = productService;
            _productImageService = productImageService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id, string? success = null)
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

            if (product.Status == ProductStatus.Archived)
            {
                return RedirectToPage("/Seller/Products", new { success = "This product has been deleted and cannot be edited." });
            }

            ProductId = id;
            ProductName = product.Name;
            SuccessMessage = success;

            await LoadImagesAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(Guid id, IFormFile? imageFile, bool setAsMain = false)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            ProductId = id;

            if (imageFile == null || imageFile.Length == 0)
            {
                Errors = new[] { "Please select an image file to upload." };
                await LoadProductAndImagesAsync(id);
                return Page();
            }

            var command = new UploadProductImageCommand(
                id,
                userId,
                imageFile.OpenReadStream(),
                imageFile.FileName,
                imageFile.ContentType,
                imageFile.Length,
                setAsMain);

            var result = await _productImageService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Seller {UserId} uploaded image {ImageId} to product {ProductId}",
                    userId,
                    result.Image?.Id,
                    id);

                return RedirectToPage(new { id, success = "Image uploaded successfully." });
            }

            Errors = result.Errors;
            await LoadProductAndImagesAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostSetMainAsync(Guid id, Guid imageId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            ProductId = id;

            var command = new SetMainProductImageCommand(imageId, userId);
            var result = await _productImageService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Seller {UserId} set image {ImageId} as main for product {ProductId}",
                    userId,
                    imageId,
                    id);

                return RedirectToPage(new { id, success = "Main image updated successfully." });
            }

            Errors = result.Errors;
            await LoadProductAndImagesAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid imageId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            ProductId = id;

            var command = new DeleteProductImageCommand(imageId, userId);
            var result = await _productImageService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Seller {UserId} deleted image {ImageId} from product {ProductId}",
                    userId,
                    imageId,
                    id);

                return RedirectToPage(new { id, success = "Image deleted successfully." });
            }

            Errors = result.Errors;
            await LoadProductAndImagesAsync(id);
            return Page();
        }

        private async Task LoadProductAndImagesAsync(Guid productId)
        {
            var product = await _productService.HandleAsync(new GetProductByIdQuery(productId));
            if (product is not null)
            {
                ProductName = product.Name;
            }

            await LoadImagesAsync(productId);
        }

        private async Task LoadImagesAsync(Guid productId)
        {
            var images = await _productImageService.HandleAsync(new GetProductImagesQuery(productId));
            Images = images.Select(MapToViewModel).ToArray();
        }

        private static ProductImageViewModel MapToViewModel(ProductImageDto dto)
        {
            return new ProductImageViewModel(
                dto.Id,
                dto.ProductId,
                dto.FileName,
                dto.ImageUrl,
                dto.ThumbnailUrl,
                dto.IsMain,
                dto.DisplayOrder,
                dto.CreatedAt);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
