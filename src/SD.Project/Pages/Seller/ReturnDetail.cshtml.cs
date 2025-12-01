using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for displaying and managing a return request.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class ReturnDetailModel : PageModel
{
    private readonly ILogger<ReturnDetailModel> _logger;
    private readonly ReturnRequestService _returnRequestService;
    private readonly StoreService _storeService;

    public string? StoreName { get; private set; }
    public SellerReturnRequestDetailsViewModel? ReturnRequest { get; private set; }
    public Guid StoreId { get; private set; }

    [BindProperty]
    public string? SellerResponse { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Rejection reason is required")]
    public string? RejectionReason { get; set; }

    public ReturnDetailModel(
        ILogger<ReturnDetailModel> logger,
        ReturnRequestService returnRequestService,
        StoreService storeService)
    {
        _logger = logger;
        _returnRequestService = returnRequestService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        // Get seller's store
        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Store not found for seller {SellerId}", userId);
            return Page();
        }

        StoreName = store.Name;
        StoreId = store.Id;

        // Get return request details
        var returnRequest = await _returnRequestService.HandleAsync(
            new GetSellerReturnRequestDetailsQuery(store.Id, returnRequestId),
            cancellationToken);

        if (returnRequest is null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found for store {StoreId}", returnRequestId, store.Id);
            return Page();
        }

        var requestItems = returnRequest.RequestItems.Select(i => new ReturnRequestItemViewModel(
            i.ItemId,
            i.OrderItemId,
            i.ProductName,
            i.Quantity)).ToList();

        ReturnRequest = new SellerReturnRequestDetailsViewModel(
            returnRequest.ReturnRequestId,
            returnRequest.OrderId,
            returnRequest.ShipmentId,
            returnRequest.CaseNumber,
            returnRequest.OrderNumber,
            returnRequest.Type,
            returnRequest.Status,
            returnRequest.BuyerName,
            returnRequest.BuyerEmail,
            returnRequest.Reason,
            returnRequest.Comments,
            returnRequest.SellerResponse,
            returnRequest.SubOrderTotal,
            returnRequest.Currency,
            returnRequest.CreatedAt,
            returnRequest.ApprovedAt,
            returnRequest.RejectedAt,
            returnRequest.CompletedAt,
            returnRequest.Items.Select(i => new SellerSubOrderItemViewModel(
                i.ItemId,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.LineTotal,
                i.ShippingMethodName)).ToList().AsReadOnly(),
            requestItems.AsReadOnly());

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            TempData["Error"] = "Store not found.";
            return RedirectToPage(new { returnRequestId });
        }

        var result = await _returnRequestService.HandleAsync(
            new ApproveReturnRequestCommand(store.Id, returnRequestId, SellerResponse),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to approve return request.";
            _logger.LogWarning("Failed to approve return request {ReturnRequestId}: {Error}", returnRequestId, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = "Return request approved. The buyer will be notified.";
            _logger.LogInformation("Return request {ReturnRequestId} approved by seller {UserId}", returnRequestId, userId);
        }

        return RedirectToPage(new { returnRequestId });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            TempData["Error"] = "Store not found.";
            return RedirectToPage(new { returnRequestId });
        }

        if (string.IsNullOrWhiteSpace(RejectionReason))
        {
            TempData["Error"] = "Rejection reason is required.";
            return RedirectToPage(new { returnRequestId });
        }

        var result = await _returnRequestService.HandleAsync(
            new RejectReturnRequestCommand(store.Id, returnRequestId, RejectionReason),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to reject return request.";
            _logger.LogWarning("Failed to reject return request {ReturnRequestId}: {Error}", returnRequestId, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = "Return request rejected. The buyer will be notified.";
            _logger.LogInformation("Return request {ReturnRequestId} rejected by seller {UserId}", returnRequestId, userId);
        }

        return RedirectToPage(new { returnRequestId });
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid returnRequestId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            TempData["Error"] = "Store not found.";
            return RedirectToPage(new { returnRequestId });
        }

        var result = await _returnRequestService.HandleAsync(
            new CompleteReturnRequestCommand(store.Id, returnRequestId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to complete return request.";
            _logger.LogWarning("Failed to complete return request {ReturnRequestId}: {Error}", returnRequestId, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = "Return completed. The buyer will be notified.";
            _logger.LogInformation("Return request {ReturnRequestId} completed by seller {UserId}", returnRequestId, userId);
        }

        return RedirectToPage(new { returnRequestId });
    }
}
