using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ProductService _productService;

        public IReadOnlyCollection<ProductViewModel> Products { get; private set; } = Array.Empty<ProductViewModel>();

        public IndexModel(ILogger<IndexModel> logger, ProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        /// <summary>
        /// Loads product data for the dashboard.
        /// </summary>
        public async Task OnGetAsync()
        {
            var items = await _productService.HandleAsync(new GetAllProductsQuery());
            Products = items
                .Select(p => new ProductViewModel(p.Id, p.Name, p.Amount, p.Currency, p.IsActive))
                .ToArray();
            _logger.LogDebug("Loaded {Count} products for display", Products.Count);
        }
    }
}
