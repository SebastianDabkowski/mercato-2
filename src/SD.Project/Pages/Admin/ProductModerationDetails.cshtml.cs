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

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin)]
public class ProductModerationDetailsModel : PageModel
{
    private readonly ILogger<ProductModerationDetailsModel> _logger;
    private readonly ProductModerationService _productModerationService;
    private readonly ProductImageService _productImageService;

    public ProductModerationViewModel? Product { get; private set; }
    public IReadOnlyCollection<ProductModerationAuditLogViewModel> AuditHistory { get; private set; } = Array.Empty<ProductModerationAuditLogViewModel>();
    public IReadOnlyCollection<ProductImageDto> ProductImages { get; private set; } = Array.Empty<ProductImageDto>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public ProductModerationDetailsModel(
        ILogger<ProductModerationDetailsModel> logger,
        ProductModerationService productModerationService,
        ProductImageService productImageService)
    {
        _logger = logger;
        _productModerationService = productModerationService;
        _productImageService = productImageService;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, string? success = null, string? error = null)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Admin/ProductModeration", new { error = "Invalid product ID." });
        }

        SuccessMessage = success;
        ErrorMessage = error;

        await LoadProductAsync(id);
        if (Product is null)
        {
            return RedirectToPage("/Admin/ProductModeration", new { error = "Product not found." });
        }

        await LoadAuditHistoryAsync(id);
        await LoadProductImagesAsync(id);

        _logger.LogInformation(
            "Admin {UserId} viewed product moderation details for product {ProductId}",
            GetUserId(),
            id);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            return RedirectToPage(new { id = productId, error = "Invalid product ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = productId, error = "Unable to determine admin user." });
        }

        var command = new ApproveProductCommand(productId, userId);
        var result = await _productModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved product {ProductId}",
                GetUserId(),
                productId);

            return RedirectToPage(new { id = productId, success = "Product approved successfully." });
        }

        return RedirectToPage(new { id = productId, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid productId, string reason)
    {
        if (productId == Guid.Empty)
        {
            return RedirectToPage(new { id = productId, error = "Invalid product ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { id = productId, error = "Rejection reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = productId, error = "Unable to determine admin user." });
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

            return RedirectToPage(new { id = productId, success = "Product rejected successfully." });
        }

        return RedirectToPage(new { id = productId, error = result.ErrorMessage });
    }

    private async Task LoadProductAsync(Guid productId)
    {
        var productDto = await _productModerationService.HandleAsync(
            new GetProductForModerationDetailsQuery(productId));

        if (productDto is null)
        {
            return;
        }

        Product = new ProductModerationViewModel(
            productDto.ProductId,
            productDto.StoreId,
            productDto.Name,
            productDto.Description,
            productDto.Price,
            productDto.Currency,
            productDto.Stock,
            productDto.Category,
            productDto.Status.ToString(),
            productDto.ModerationStatus.ToString(),
            productDto.ModerationRejectionReason,
            productDto.LastModeratorId,
            productDto.LastModeratorName,
            productDto.LastModeratedAt,
            productDto.StoreName,
            productDto.SellerName,
            productDto.SellerEmail,
            productDto.MainImageUrl,
            productDto.CreatedAt,
            productDto.UpdatedAt);
    }

    private async Task LoadAuditHistoryAsync(Guid productId)
    {
        var auditLogs = await _productModerationService.HandleAsync(
            new GetProductModerationHistoryQuery(productId));

        AuditHistory = auditLogs.Select(a => new ProductModerationAuditLogViewModel(
            a.Id,
            a.ProductId,
            a.ModeratorId,
            a.ModeratorName,
            a.Decision.ToString(),
            a.Reason,
            a.CreatedAt)).ToList();
    }

    private async Task LoadProductImagesAsync(Guid productId)
    {
        ProductImages = await _productImageService.HandleAsync(
            new GetProductImagesQuery(productId));
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
