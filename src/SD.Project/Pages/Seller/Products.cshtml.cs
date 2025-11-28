using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class ProductsModel : PageModel
    {
        private readonly ILogger<ProductsModel> _logger;
        private readonly ProductService _productService;
        private readonly StoreService _storeService;

        public IReadOnlyCollection<ProductViewModel> Products { get; private set; } = Array.Empty<ProductViewModel>();
        public bool HasStore { get; private set; }
        public string? SuccessMessage { get; private set; }

        public ProductsModel(
            ILogger<ProductsModel> logger,
            ProductService productService,
            StoreService storeService)
        {
            _logger = logger;
            _productService = productService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(string? success = null)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                HasStore = false;
                return RedirectToPage("/Seller/StoreSettings");
            }

            HasStore = true;
            SuccessMessage = success;

            var products = await _productService.HandleAsync(new GetAllProductsByStoreIdQuery(store.Id));
            Products = products
                .Select(p => new ProductViewModel(
                    p.Id,
                    p.Name,
                    p.Amount,
                    p.Currency,
                    p.Stock,
                    p.Category,
                    p.Status,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt))
                .ToArray();

            _logger.LogInformation(
                "Seller {UserId} viewed products for store {StoreId}, found {ProductCount} products",
                userId,
                store.Id,
                Products.Count);

            return Page();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
