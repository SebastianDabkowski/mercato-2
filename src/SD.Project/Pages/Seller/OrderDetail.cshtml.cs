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

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for displaying seller's sub-order details and managing fulfilment status.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class OrderDetailModel : PageModel
{
    private readonly ILogger<OrderDetailModel> _logger;
    private readonly OrderService _orderService;
    private readonly StoreService _storeService;
    private readonly ReturnRequestService _returnRequestService;

    public SellerSubOrderDetailsViewModel? SubOrder { get; private set; }
    public string? StoreName { get; private set; }
    public ShipmentStatusTransitionsDto? StatusTransitions { get; private set; }
    
    /// <summary>
    /// Return request for this sub-order, if one exists.
    /// </summary>
    public SellerReturnRequestDetailsViewModel? ReturnRequest { get; private set; }

    // Properties for update tracking form
    [BindProperty]
    public string? CarrierName { get; set; }

    [BindProperty]
    public string? TrackingNumber { get; set; }

    [BindProperty]
    public string? TrackingUrl { get; set; }

    public OrderDetailModel(
        ILogger<OrderDetailModel> logger,
        OrderService orderService,
        StoreService storeService,
        ReturnRequestService returnRequestService)
    {
        _logger = logger;
        _orderService = orderService;
        _storeService = storeService;
        _returnRequestService = returnRequestService;
    }

    public async Task<IActionResult> OnGetAsync(Guid subOrderId, CancellationToken cancellationToken = default)
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

        // Get sub-order details
        var subOrder = await _orderService.HandleAsync(
            new GetSellerSubOrderDetailsQuery(store.Id, subOrderId),
            cancellationToken);

        if (subOrder is null)
        {
            _logger.LogWarning("Sub-order {SubOrderId} not found for store {StoreId}", subOrderId, store.Id);
            return Page();
        }

        // Get available status transitions
        StatusTransitions = await _orderService.GetShipmentStatusTransitionsAsync(
            store.Id,
            subOrderId,
            cancellationToken);

        SubOrder = new SellerSubOrderDetailsViewModel(
            subOrder.SubOrderId,
            subOrder.OrderId,
            subOrder.OrderNumber,
            subOrder.Status,
            subOrder.PaymentStatus,
            subOrder.Subtotal,
            subOrder.ShippingCost,
            subOrder.Total,
            subOrder.Currency,
            subOrder.BuyerName,
            subOrder.BuyerEmail,
            subOrder.BuyerPhone,
            subOrder.DeliveryAddress,
            subOrder.DeliveryInstructions,
            subOrder.ShippingMethodName,
            subOrder.Items.Select(i => new SellerSubOrderItemViewModel(
                i.ItemId,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.LineTotal,
                i.ShippingMethodName)).ToList().AsReadOnly(),
            subOrder.CreatedAt,
            subOrder.PaidAt,
            subOrder.ShippedAt,
            subOrder.DeliveredAt,
            subOrder.CancelledAt,
            subOrder.RefundedAt,
            subOrder.CarrierName,
            subOrder.TrackingNumber,
            subOrder.TrackingUrl);

        // Pre-fill tracking form with existing data
        CarrierName = subOrder.CarrierName;
        TrackingNumber = subOrder.TrackingNumber;
        TrackingUrl = subOrder.TrackingUrl;

        // Check for return request on this sub-order
        var returnDetails = await _returnRequestService.HandleAsync(
            new GetSellerReturnRequestByShipmentQuery(store.Id, subOrderId),
            cancellationToken);
        
        if (returnDetails is not null)
        {
            ReturnRequest = new SellerReturnRequestDetailsViewModel(
                returnDetails.ReturnRequestId,
                returnDetails.OrderId,
                returnDetails.ShipmentId,
                returnDetails.OrderNumber,
                returnDetails.Status,
                returnDetails.BuyerName,
                returnDetails.BuyerEmail,
                returnDetails.Reason,
                returnDetails.Comments,
                returnDetails.SellerResponse,
                returnDetails.SubOrderTotal,
                returnDetails.Currency,
                returnDetails.CreatedAt,
                returnDetails.ApprovedAt,
                returnDetails.RejectedAt,
                returnDetails.CompletedAt,
                returnDetails.Items.Select(i => new SellerSubOrderItemViewModel(
                    i.ItemId,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.LineTotal,
                    i.ShippingMethodName)).ToList().AsReadOnly());
        }

        _logger.LogInformation("Seller order detail page accessed for order {OrderNumber}, sub-order {SubOrderId}",
            SubOrder.OrderNumber, SubOrder.SubOrderId);

        return Page();
    }

    /// <summary>
    /// Handles status update to 'Processing' (preparing).
    /// </summary>
    public async Task<IActionResult> OnPostStartPreparingAsync(Guid subOrderId, CancellationToken cancellationToken = default)
    {
        return await UpdateStatusAsync(subOrderId, "Processing", cancellationToken);
    }

    /// <summary>
    /// Handles status update to 'Shipped'.
    /// </summary>
    public async Task<IActionResult> OnPostShipAsync(Guid subOrderId, CancellationToken cancellationToken = default)
    {
        return await UpdateStatusWithTrackingAsync(subOrderId, "Shipped", cancellationToken);
    }

    /// <summary>
    /// Handles status update to 'Delivered'.
    /// </summary>
    public async Task<IActionResult> OnPostMarkDeliveredAsync(Guid subOrderId, CancellationToken cancellationToken = default)
    {
        return await UpdateStatusAsync(subOrderId, "Delivered", cancellationToken);
    }

    /// <summary>
    /// Handles cancellation of the shipment.
    /// </summary>
    public async Task<IActionResult> OnPostCancelAsync(Guid subOrderId, CancellationToken cancellationToken = default)
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
            return RedirectToPage(new { subOrderId });
        }

        var command = new CancelShipmentCommand(store.Id, subOrderId, userId);
        var result = await _orderService.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to cancel the order.";
            _logger.LogWarning("Failed to cancel sub-order {SubOrderId}: {Error}", subOrderId, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = "Order has been cancelled successfully.";
            _logger.LogInformation("Sub-order {SubOrderId} cancelled by seller {UserId}", subOrderId, userId);
        }

        return RedirectToPage(new { subOrderId });
    }

    /// <summary>
    /// Handles updating tracking information for a shipped order.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateTrackingAsync(Guid subOrderId, CancellationToken cancellationToken = default)
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
            return RedirectToPage(new { subOrderId });
        }

        var command = new UpdateTrackingInfoCommand(
            store.Id,
            subOrderId,
            userId,
            CarrierName?.Trim(),
            TrackingNumber?.Trim(),
            TrackingUrl?.Trim());

        var result = await _orderService.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to update tracking information.";
            _logger.LogWarning("Failed to update tracking for sub-order {SubOrderId}: {Error}", subOrderId, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = "Tracking information updated successfully.";
            _logger.LogInformation("Tracking info updated for sub-order {SubOrderId} by seller {UserId}", subOrderId, userId);
        }

        return RedirectToPage(new { subOrderId });
    }

    private async Task<IActionResult> UpdateStatusAsync(Guid subOrderId, string newStatus, CancellationToken cancellationToken)
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
            return RedirectToPage(new { subOrderId });
        }

        var command = new UpdateShipmentStatusCommand(store.Id, subOrderId, newStatus, userId);
        var result = await _orderService.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? $"Failed to update status to {newStatus}.";
            _logger.LogWarning("Failed to update sub-order {SubOrderId} to {Status}: {Error}",
                subOrderId, newStatus, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = $"Order status updated to {newStatus}.";
            _logger.LogInformation("Sub-order {SubOrderId} status changed from {OldStatus} to {NewStatus} by seller {UserId}",
                subOrderId, result.PreviousStatus, result.NewStatus, userId);
        }

        return RedirectToPage(new { subOrderId });
    }

    private async Task<IActionResult> UpdateStatusWithTrackingAsync(Guid subOrderId, string newStatus, CancellationToken cancellationToken)
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
            return RedirectToPage(new { subOrderId });
        }

        var command = new UpdateShipmentStatusCommand(
            store.Id,
            subOrderId,
            newStatus,
            userId,
            CarrierName?.Trim(),
            TrackingNumber?.Trim(),
            TrackingUrl?.Trim());

        var result = await _orderService.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? $"Failed to update status to {newStatus}.";
            _logger.LogWarning("Failed to update sub-order {SubOrderId} to {Status}: {Error}",
                subOrderId, newStatus, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = $"Order status updated to {newStatus}.";
            _logger.LogInformation("Sub-order {SubOrderId} status changed from {OldStatus} to {NewStatus} by seller {UserId}",
                subOrderId, result.PreviousStatus, result.NewStatus, userId);
        }

        return RedirectToPage(new { subOrderId });
    }
}
