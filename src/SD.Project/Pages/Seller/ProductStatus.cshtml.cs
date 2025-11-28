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
    public class ProductStatusModel : PageModel
    {
        private readonly ILogger<ProductStatusModel> _logger;
        private readonly ProductService _productService;
        private readonly StoreService _storeService;

        public ProductViewModel? Product { get; private set; }
        public IReadOnlyList<string> AvailableTransitions { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string>? ValidationErrors { get; private set; }
        public string? SuccessMessage { get; private set; }

        [BindProperty]
        public ProductStatus TargetStatus { get; set; }

        public ProductStatusModel(
            ILogger<ProductStatusModel> logger,
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

            Product = MapToViewModel(product);
            AvailableTransitions = GetAvailableTransitions(product.Status);

            _logger.LogInformation(
                "Seller {UserId} viewing status change options for product {ProductId}",
                userId, id);

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

            // Reload product for display
            var productDto = await _productService.HandleAsync(new GetProductByIdQuery(id));
            if (productDto is null)
            {
                return NotFound();
            }

            // Check if user is admin for override capability
            var isAdmin = User.IsInRole(UserRole.Admin.ToString());

            var command = new ChangeProductStatusCommand(id, userId, TargetStatus, isAdmin);
            var result = await _productService.HandleAsync(command);

            if (!result.Success)
            {
                ValidationErrors = result.Errors;
                Product = MapToViewModel(productDto);
                AvailableTransitions = GetAvailableTransitions(productDto.Status);
                return Page();
            }

            _logger.LogInformation(
                "Seller {UserId} changed product {ProductId} status to {NewStatus}",
                userId, id, TargetStatus);

            return RedirectToPage("/Seller/Products", new { success = $"Product status changed to {TargetStatus}." });
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private static ProductViewModel MapToViewModel(Application.DTOs.ProductDto dto)
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
                dto.HeightCm);
        }

        private static IReadOnlyList<string> GetAvailableTransitions(ProductStatus currentStatus)
        {
            return currentStatus switch
            {
                ProductStatus.Draft => new[] { ProductStatus.Active.ToString(), ProductStatus.Archived.ToString() },
                ProductStatus.Active => new[] { ProductStatus.Suspended.ToString(), ProductStatus.Archived.ToString() },
                ProductStatus.Inactive => new[] { ProductStatus.Active.ToString(), ProductStatus.Suspended.ToString(), ProductStatus.Archived.ToString() },
                ProductStatus.Suspended => new[] { ProductStatus.Active.ToString(), ProductStatus.Archived.ToString() },
                ProductStatus.Archived => Array.Empty<string>(),
                _ => Array.Empty<string>()
            };
        }
    }
}
