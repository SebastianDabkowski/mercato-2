using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin, UserRole.Compliance)]
public class ProductModerationModel : PageModel
{
    private readonly ILogger<ProductModerationModel> _logger;
    private readonly ProductModerationService _productModerationService;

    public IReadOnlyCollection<ProductModerationViewModel> Products { get; private set; } = Array.Empty<ProductModerationViewModel>();
    public ProductModerationStatsViewModel? Stats { get; private set; }
    public IReadOnlyCollection<SelectListItem> StatusOptions { get; private set; } = Array.Empty<SelectListItem>();

    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;
    public int TotalCount { get; private set; }
    public int PageSize { get; private set; } = 20;

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Filter parameters
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public ProductModerationModel(
        ILogger<ProductModerationModel> logger,
        ProductModerationService productModerationService)
    {
        _logger = logger;
        _productModerationService = productModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        LoadFilters();
        await LoadStatsAsync();
        await LoadProductsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed product moderation, found {ProductCount} products",
            GetUserId(),
            TotalCount);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid product ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new ApproveProductCommand(productId, userId);
        var result = await _productModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved product {ProductId}",
                GetUserId(),
                productId);

            return RedirectToPage(new { success = "Product approved successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid productId, string reason)
    {
        if (productId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid product ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Rejection reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new RejectProductCommand(productId, userId, reason);
        var result = await _productModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} rejected product {ProductId} with reason {Reason}",
                GetUserId(),
                productId,
                reason);

            return RedirectToPage(new { success = "Product rejected successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostBatchApproveAsync(string productIds)
    {
        if (string.IsNullOrWhiteSpace(productIds))
        {
            return RedirectToPage(new { error = "No products selected." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = productIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid product IDs provided." });
        }

        var command = new BatchApproveProductsCommand(ids, userId);
        var result = await _productModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch approved {SuccessCount}/{TotalCount} products",
            GetUserId(),
            result.SuccessCount,
            ids.Count);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully approved {result.SuccessCount} product(s)." });
        }

        return RedirectToPage(new { error = $"Approved {result.SuccessCount} product(s), {result.FailureCount} failed." });
    }

    public async Task<IActionResult> OnPostBatchRejectAsync(string productIds, string reason)
    {
        if (string.IsNullOrWhiteSpace(productIds))
        {
            return RedirectToPage(new { error = "No products selected." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Rejection reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = productIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid product IDs provided." });
        }

        var command = new BatchRejectProductsCommand(ids, userId, reason);
        var result = await _productModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch rejected {SuccessCount}/{TotalCount} products with reason {Reason}",
            GetUserId(),
            result.SuccessCount,
            ids.Count,
            reason);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully rejected {result.SuccessCount} product(s)." });
        }

        return RedirectToPage(new { error = $"Rejected {result.SuccessCount} product(s), {result.FailureCount} failed." });
    }

    private async Task LoadStatsAsync()
    {
        var statsDto = await _productModerationService.HandleAsync(
            new GetProductModerationStatsQuery());

        Stats = new ProductModerationStatsViewModel(
            statsDto.PendingCount,
            statsDto.ApprovedCount,
            statsDto.RejectedCount,
            statsDto.ApprovedTodayCount,
            statsDto.RejectedTodayCount);
    }

    private async Task LoadProductsAsync()
    {
        ProductModerationStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<ProductModerationStatus>(Status, out var parsed))
        {
            statusFilter = parsed;
        }

        var query = new GetProductsForModerationQuery(
            statusFilter,
            Category,
            SearchTerm,
            Page,
            PageSize);

        var result = await _productModerationService.HandleAsync(query);

        Products = result.Items.Select(p => new ProductModerationViewModel(
            p.ProductId,
            p.StoreId,
            p.Name,
            p.Description,
            p.Price,
            p.Currency,
            p.Stock,
            p.Category,
            p.Status.ToString(),
            p.ModerationStatus.ToString(),
            p.ModerationRejectionReason,
            p.LastModeratorId,
            p.LastModeratorName,
            p.LastModeratedAt,
            p.StoreName,
            p.SellerName,
            p.SellerEmail,
            p.MainImageUrl,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        CurrentPage = result.PageNumber;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
    }

    private void LoadFilters()
    {
        StatusOptions = new List<SelectListItem>
        {
            new SelectListItem("All Statuses", ""),
            new SelectListItem("Pending Review", "PendingReview"),
            new SelectListItem("Approved", "Approved"),
            new SelectListItem("Rejected", "Rejected")
        };
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
