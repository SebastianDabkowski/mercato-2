using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Seller;

[RequireRole(UserRole.Seller, UserRole.Admin)]
public class ExportProductsModel : PageModel
{
    private readonly ILogger<ExportProductsModel> _logger;
    private readonly ProductExportService _exportService;
    private readonly StoreService _storeService;
    private readonly CategoryService _categoryService;

    public bool HasStore { get; private set; }
    public IReadOnlyCollection<string> Categories { get; private set; } = Array.Empty<string>();
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public string Format { get; set; } = "csv";

    [BindProperty]
    public string? SearchTerm { get; set; }

    [BindProperty]
    public string? CategoryFilter { get; set; }

    [BindProperty]
    public bool ActiveOnly { get; set; }

    public ExportProductsModel(
        ILogger<ExportProductsModel> logger,
        ProductExportService exportService,
        StoreService storeService,
        CategoryService categoryService)
    {
        _logger = logger;
        _exportService = exportService;
        _storeService = storeService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> OnGetAsync()
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
        await LoadCategoriesAsync();

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

        // Parse format
        if (!Enum.TryParse<ExportFormat>(Format, ignoreCase: true, out var exportFormat))
        {
            exportFormat = ExportFormat.Csv;
        }

        var query = new ExportProductsQuery(
            userId,
            exportFormat,
            SearchTerm,
            CategoryFilter,
            ActiveOnly ? true : null);

        var result = await _exportService.HandleAsync(query);

        if (!result.IsSuccess)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            await LoadCategoriesAsync();
            return Page();
        }

        _logger.LogInformation(
            "Seller {UserId} exported {ProductCount} products in {Format} format",
            userId,
            result.ExportedCount,
            exportFormat);

        return File(result.FileData!, result.ContentType!, result.FileName);
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery());
        Categories = categories.Select(c => c.Name).ToArray();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
