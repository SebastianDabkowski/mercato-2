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
    public class BulkUpdateModel : PageModel
    {
        private readonly ILogger<BulkUpdateModel> _logger;
        private readonly ProductService _productService;
        private readonly StoreService _storeService;

        public IReadOnlyCollection<ProductViewModel> Products { get; private set; } = Array.Empty<ProductViewModel>();
        public BulkUpdateResultDto? Result { get; private set; }
        public bool HasStore { get; private set; }

        [BindProperty]
        public List<Guid> SelectedProductIds { get; set; } = new();

        [BindProperty]
        public string PriceChangeType { get; set; } = "None";

        [BindProperty]
        public decimal? PriceValue { get; set; }

        [BindProperty]
        public string StockChangeType { get; set; } = "None";

        [BindProperty]
        public int? StockValue { get; set; }

        public BulkUpdateModel(
            ILogger<BulkUpdateModel> logger,
            ProductService productService,
            StoreService storeService)
        {
            _logger = logger;
            _productService = productService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(string? ids = null)
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

            // Parse pre-selected product IDs from query string
            if (!string.IsNullOrEmpty(ids))
            {
                SelectedProductIds = ids.Split(',')
                    .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();
            }

            await LoadProductsAsync(store.Id);

            _logger.LogInformation(
                "Seller {UserId} accessed bulk update page for store {StoreId} with {ProductCount} products",
                userId,
                store.Id,
                Products.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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

            // Re-load products for display
            await LoadProductsAsync(store.Id);

            // Parse change types
            var priceChangeTypeEnum = PriceChangeType switch
            {
                "FixedValue" => Application.Commands.PriceChangeType.FixedValue,
                "PercentageUp" => Application.Commands.PriceChangeType.PercentageUp,
                "PercentageDown" => Application.Commands.PriceChangeType.PercentageDown,
                _ => Application.Commands.PriceChangeType.None
            };

            var stockChangeTypeEnum = StockChangeType switch
            {
                "SetExact" => Application.Commands.StockChangeType.SetExact,
                "Increase" => Application.Commands.StockChangeType.Increase,
                "Decrease" => Application.Commands.StockChangeType.Decrease,
                _ => Application.Commands.StockChangeType.None
            };

            var command = new BulkUpdatePriceAndStockCommand(
                userId,
                SelectedProductIds,
                priceChangeTypeEnum,
                PriceValue,
                stockChangeTypeEnum,
                StockValue);

            _logger.LogInformation(
                "Seller {UserId} performing bulk update on {ProductCount} products",
                userId,
                SelectedProductIds.Count);

            Result = await _productService.HandleAsync(command);

            if (Result.IsSuccess && Result.SuccessCount > 0)
            {
                _logger.LogInformation(
                    "Bulk update completed for seller {UserId}: {SuccessCount} succeeded, {FailureCount} failed",
                    userId,
                    Result.SuccessCount,
                    Result.FailureCount);
            }

            return Page();
        }

        private async Task LoadProductsAsync(Guid storeId)
        {
            var products = await _productService.HandleAsync(new GetAllProductsByStoreIdQuery(storeId));
            Products = products
                .Where(p => p.Status != ProductStatus.Archived)
                .Select(p => new ProductViewModel(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Amount,
                    p.Currency,
                    p.Stock,
                    p.Category,
                    p.Status,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.WeightKg,
                    p.LengthCm,
                    p.WidthCm,
                    p.HeightCm))
                .ToArray();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
