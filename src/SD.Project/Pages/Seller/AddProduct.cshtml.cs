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
    public class AddProductModel : PageModel
    {
        private readonly ILogger<AddProductModel> _logger;
        private readonly ProductService _productService;
        private readonly StoreService _storeService;

        [BindProperty]
        public CreateProductViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool HasStore { get; private set; }

        public AddProductModel(
            ILogger<AddProductModel> logger,
            ProductService productService,
            StoreService storeService)
        {
            _logger = logger;
            _productService = productService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            HasStore = store is not null;

            if (!HasStore)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

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

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var command = new CreateProductCommand(
                store.Id,
                Input.Name.Trim(),
                Input.Price,
                Input.Currency.Trim().ToUpperInvariant(),
                Input.Stock,
                Input.Category.Trim());

            var result = await _productService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Seller {UserId} created product {ProductId} in store {StoreId}",
                    userId,
                    result.Product?.Id,
                    store.Id);

                return RedirectToPage("/Seller/Products", new { success = "Product created successfully." });
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
