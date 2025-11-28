using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for fetching recently viewed products.
/// </summary>
public class RecentlyViewedModel : PageModel
{
    private readonly ProductService _productService;
    private const int MaxProducts = 10;

    public RecentlyViewedModel(ProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery(Name = "ids")] string? ids,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return new JsonResult(Array.Empty<object>());
        }

        // Parse the comma-separated IDs
        var productIds = ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => id.Trim())
            .Where(id => Guid.TryParse(id, out _))
            .Select(id => Guid.Parse(id))
            .Take(MaxProducts)
            .ToList();

        if (productIds.Count == 0)
        {
            return new JsonResult(Array.Empty<object>());
        }

        var products = await _productService.HandleAsync(
            new GetRecentlyViewedProductsQuery(productIds),
            cancellationToken);

        // Return minimal product data for the recently viewed section
        var result = products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            amount = p.Amount,
            currency = p.Currency,
            mainImageThumbnailUrl = p.MainImageThumbnailUrl
        });

        return new JsonResult(result);
    }
}
