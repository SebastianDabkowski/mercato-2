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
    private readonly ShippingLabelService _shippingLabelService;

    public SellerSubOrderDetailsViewModel? SubOrder { get; private set; }
    public string? StoreName { get; private set; }
    public ShipmentStatusTransitionsDto? StatusTransitions { get; private set; }
    
    /// <summary>
    /// Return request for this sub-order, if one exists.
    /// </summary>
    public SellerReturnRequestDetailsViewModel? ReturnRequest { get; private set; }

    /// <summary>
    /// Shipping label for this sub-order, if one exists.
    /// </summary>
    public ShippingLabelViewModel? ShippingLabel { get; private set; }

    /// <summary>
    /// Whether label generation is available for this shipment.
    /// </summary>
    public bool CanGenerateLabel { get; private set; }

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
        ReturnRequestService returnRequestService,
        ShippingLabelService shippingLabelService)
    {
        _logger = logger;
        _orderService = orderService;
        _storeService = storeService;
        _returnRequestService = returnRequestService;
        _shippingLabelService = shippingLabelService;
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
            var requestItems = returnDetails.RequestItems.Select(i => new ReturnRequestItemViewModel(
                i.ItemId,
                i.OrderItemId,
                i.ProductName,
                i.Quantity)).ToList();

            ReturnRequest = new SellerReturnRequestDetailsViewModel(
                returnDetails.ReturnRequestId,
                returnDetails.OrderId,
                returnDetails.ShipmentId,
                returnDetails.CaseNumber,
                returnDetails.OrderNumber,
                returnDetails.Type,
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
                    i.ShippingMethodName)).ToList().AsReadOnly(),
                requestItems.AsReadOnly());
        }

        // Load existing shipping label if available
        var labelDto = await _shippingLabelService.HandleAsync(
            new GetShippingLabelByShipmentQuery(store.Id, subOrderId),
            cancellationToken);

        if (labelDto is not null)
        {
            ShippingLabel = new ShippingLabelViewModel(
                labelDto.LabelId,
                labelDto.ShipmentId,
                labelDto.Format,
                labelDto.LabelSize,
                labelDto.TrackingNumber,
                labelDto.CarrierName,
                labelDto.GeneratedAt,
                labelDto.ExpiresAt,
                labelDto.IsValid,
                labelDto.IsVoided,
                labelDto.AccessCount);
        }

        // Determine if label generation is available
        // Label can be generated for shipped orders that have a provider shipment ID
        CanGenerateLabel = SubOrder.Status == "Shipped" && 
                          !string.IsNullOrEmpty(SubOrder.CarrierName) &&
                          ShippingLabel is null;

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

    /// <summary>
    /// Handles generating a shipping label for the shipment.
    /// </summary>
    public async Task<IActionResult> OnPostGenerateLabelAsync(Guid subOrderId, CancellationToken cancellationToken = default)
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

        var command = new GenerateShippingLabelCommand(store.Id, subOrderId, userId);
        var result = await _shippingLabelService.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to generate shipping label.";
            _logger.LogWarning("Failed to generate label for sub-order {SubOrderId}: {Error}",
                subOrderId, result.ErrorMessage);
        }
        else
        {
            TempData["Success"] = "Shipping label generated successfully. Click Download to get the label.";
            _logger.LogInformation("Shipping label {LabelId} generated for sub-order {SubOrderId} by seller {UserId}",
                result.Label?.LabelId, subOrderId, userId);
        }

        return RedirectToPage(new { subOrderId });
    }

    /// <summary>
    /// Handles downloading a shipping label.
    /// </summary>
    public async Task<IActionResult> OnGetDownloadLabelAsync(Guid subOrderId, Guid labelId, CancellationToken cancellationToken = default)
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

        var query = new DownloadShippingLabelQuery(store.Id, labelId);
        var result = await _shippingLabelService.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to download shipping label.";
            _logger.LogWarning("Failed to download label {LabelId} for sub-order {SubOrderId}: {Error}",
                labelId, subOrderId, result.ErrorMessage);
            return RedirectToPage(new { subOrderId });
        }

        _logger.LogInformation("Shipping label {LabelId} downloaded for sub-order {SubOrderId} by seller {UserId}",
            labelId, subOrderId, userId);

        return File(result.Data, result.ContentType ?? "application/pdf", result.FileName ?? "shipping-label.pdf");
    }
}
