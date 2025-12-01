using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;

namespace SD.Project.Pages
{
    public class StoreModel : PageModel
    {
        private readonly ILogger<StoreModel> _logger;
        private readonly StoreService _storeService;
        private readonly ProductService _productService;
        private readonly SellerRatingService _sellerRatingService;

        public StoreDto? Store { get; private set; }
        public IReadOnlyCollection<ProductDto> Products { get; private set; } = Array.Empty<ProductDto>();
        public bool StoreNotFound { get; private set; }
        public bool StoreNotAccessible { get; private set; }
        public string? AccessibilityMessage { get; private set; }
        public double AverageRating { get; private set; }
        public int RatingCount { get; private set; }

        public StoreModel(
            ILogger<StoreModel> logger,
            StoreService storeService,
            ProductService productService,
            SellerRatingService sellerRatingService)
        {
            _logger = logger;
            _storeService = storeService;
            _productService = productService;
            _sellerRatingService = sellerRatingService;
        }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                StoreNotFound = true;
                return Page();
            }

            // Try to find store by slug first
            Store = await _storeService.HandleAsync(new GetStoreBySlugQuery(slug));

            // If not found by slug, try parsing as GUID for backward compatibility
            if (Store is null && Guid.TryParse(slug, out var id))
            {
                Store = await _storeService.HandleAsync(new GetStoreByIdQuery(id));
                
                // If found by GUID, redirect to the slug-based URL for SEO
                if (Store is not null)
                {
                    return RedirectToPage("/Store", new { slug = Store.Slug });
                }
            }

            if (Store is null)
            {
                _logger.LogWarning("Store not found: {StoreSlug}", slug);
                StoreNotFound = true;
                return Page();
            }

            // Check if store is publicly visible
            if (!Store.IsPubliclyVisible)
            {
                _logger.LogInformation("Store not publicly accessible: {StoreSlug}, Status: {Status}", slug, Store.Status);
                StoreNotAccessible = true;
                AccessibilityMessage = GetAccessibilityMessage(Store.Status);
                Store = null; // Hide store details
                return Page();
            }

            // Load products for the store
            Products = await _productService.HandleAsync(new GetProductsByStoreIdQuery(Store.Id));

            // Load seller rating stats
            var ratingStats = await _sellerRatingService.HandleAsync(new GetSellerRatingStatsQuery(Store.Id));
            AverageRating = ratingStats.AverageRating;
            RatingCount = ratingStats.RatingCount;

            return Page();
        }

        private static string GetAccessibilityMessage(Domain.Entities.StoreStatus status)
        {
            return status switch
            {
                Domain.Entities.StoreStatus.PendingVerification => "This store is currently being verified and is not yet available for public viewing.",
                Domain.Entities.StoreStatus.Suspended => "This store is currently unavailable.",
                _ => "This store is not currently available for public viewing."
            };
        }
    }
}
