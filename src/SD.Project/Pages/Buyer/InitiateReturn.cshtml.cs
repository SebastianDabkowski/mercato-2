using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// View model for an item that can be selected for return/complaint.
/// </summary>
public sealed record SelectableItemViewModel(
    Guid ItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal LineTotal,
    bool HasOpenCase,
    string? OpenCaseNumber);

/// <summary>
/// Page model for initiating a return or complaint request.
/// </summary>
public class InitiateReturnModel : PageModel
{
    private readonly ILogger<InitiateReturnModel> _logger;
    private readonly OrderService _orderService;
    private readonly ReturnRequestService _returnRequestService;

    public OrderDetailsViewModel? Order { get; private set; }
    public SellerSubOrderViewModel? SubOrder { get; private set; }
    public ReturnEligibilityViewModel? Eligibility { get; private set; }
    public List<SelectableItemViewModel> SelectableItems { get; private set; } = new();
    public string? SubmittedCaseNumber { get; private set; }
    
    [BindProperty]
    [Required(ErrorMessage = "Please select a request type")]
    public string RequestType { get; set; } = "Return";

    [BindProperty]
    [Required(ErrorMessage = "Please select a reason")]
    public string Reason { get; set; } = string.Empty;

    [BindProperty]
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [BindProperty]
    public List<Guid> SelectedItemIds { get; set; } = new();

    public InitiateReturnModel(
        ILogger<InitiateReturnModel> logger,
        OrderService orderService,
        ReturnRequestService returnRequestService)
    {
        _logger = logger;
        _orderService = orderService;
        _returnRequestService = returnRequestService;
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, Guid shipmentId, CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login", new { returnUrl = $"/Buyer/InitiateReturn?orderId={orderId}&shipmentId={shipmentId}" });
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        // Check eligibility
        var eligibility = await _returnRequestService.HandleAsync(
            new CheckReturnEligibilityQuery(buyerId.Value, orderId, shipmentId),
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            TempData["Error"] = eligibility.IneligibilityReason ?? "This order is not eligible for return.";
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        // Get order details
        var orderDetails = await _orderService.HandleAsync(
            new GetBuyerOrderDetailsQuery(buyerId.Value, orderId),
            cancellationToken);

        if (orderDetails is null)
        {
            return RedirectToPage("/Buyer/Orders");
        }

        // Find the specific sub-order
        var subOrderDto = orderDetails.SellerSubOrders.FirstOrDefault(s => s.SubOrderId == shipmentId);
        if (subOrderDto is null)
        {
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        // Map to ViewModels
        Order = new OrderDetailsViewModel(
            orderDetails.OrderId,
            orderDetails.OrderNumber,
            orderDetails.Status,
            orderDetails.PaymentStatus,
            orderDetails.PaymentStatusMessage,
            orderDetails.RecipientName,
            orderDetails.DeliveryAddressSummary,
            orderDetails.PaymentMethodName,
            orderDetails.ItemSubtotal,
            orderDetails.TotalShipping,
            orderDetails.TotalAmount,
            orderDetails.Currency,
            orderDetails.CreatedAt,
            orderDetails.EstimatedDeliveryRange,
            orderDetails.SellerSubOrders.Select(s => new SellerSubOrderViewModel(
                s.SubOrderId,
                s.StoreId,
                s.StoreName,
                s.Status,
                s.Subtotal,
                s.ShippingCost,
                s.Total,
                s.Items.Select(i => new OrderItemViewModel(
                    i.ProductId,
                    i.ProductName,
                    i.StoreId,
                    i.StoreName,
                    i.UnitPrice,
                    i.Quantity,
                    i.LineTotal,
                    i.ShippingMethodName,
                    i.ShippingCost,
                    i.EstimatedDelivery)).ToList().AsReadOnly(),
                s.CarrierName,
                s.TrackingNumber,
                s.TrackingUrl,
                s.ShippedAt,
                s.DeliveredAt,
                s.EstimatedDelivery,
                s.CancelledAt,
                s.RefundedAt,
                s.RefundedAmount)).ToList().AsReadOnly(),
            orderDetails.CancelledAt,
            orderDetails.RefundedAt,
            orderDetails.RefundedAmount);

        SubOrder = Order.SellerSubOrders.First(s => s.SubOrderId == shipmentId);

        Eligibility = new ReturnEligibilityViewModel(
            eligibility.IsEligible,
            eligibility.IneligibilityReason,
            eligibility.ReturnWindowEndsAt,
            eligibility.HasExistingReturnRequest,
            eligibility.ExistingReturnStatus);

        // Build selectable items list (checking for open cases on each item)
        // Note: For now we use OrderItems from the DTO
        SelectableItems = subOrderDto.Items.Select(i => new SelectableItemViewModel(
            i.ItemId,
            i.ProductId,
            i.ProductName,
            i.Quantity,
            i.LineTotal,
            false, // Will be populated properly in a future enhancement
            null)).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid orderId, Guid shipmentId, CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login");
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        // Validate at least one item is selected
        if (SelectedItemIds.Count == 0)
        {
            ModelState.AddModelError("SelectedItemIds", "Please select at least one item.");
        }

        if (!ModelState.IsValid)
        {
            // Reload page data
            await OnGetAsync(orderId, shipmentId, cancellationToken);
            return Page();
        }

        // Get order details to build item inputs
        var orderDetails = await _orderService.HandleAsync(
            new GetBuyerOrderDetailsQuery(buyerId.Value, orderId),
            cancellationToken);

        if (orderDetails is null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToPage("/Buyer/Orders");
        }

        var subOrderDto = orderDetails.SellerSubOrders.FirstOrDefault(s => s.SubOrderId == shipmentId);
        if (subOrderDto is null)
        {
            TempData["Error"] = "Sub-order not found.";
            return RedirectToPage("/Buyer/OrderDetails", new { orderId });
        }

        // Build item inputs from selected items
        var itemInputs = subOrderDto.Items
            .Where(i => SelectedItemIds.Contains(i.ItemId))
            .Select(i => new ReturnRequestItemInput(i.ItemId, i.ProductName, i.Quantity))
            .ToList();

        if (itemInputs.Count == 0)
        {
            TempData["Error"] = "No valid items selected.";
            await OnGetAsync(orderId, shipmentId, cancellationToken);
            return Page();
        }

        // Submit return/complaint request
        var result = await _returnRequestService.HandleAsync(
            new SubmitReturnOrComplaintCommand(
                buyerId.Value,
                orderId,
                shipmentId,
                RequestType,
                Reason,
                Description,
                itemInputs.AsReadOnly()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to submit request.";
            await OnGetAsync(orderId, shipmentId, cancellationToken);
            return Page();
        }

        _logger.LogInformation("{RequestType} request {CaseNumber} created for order {OrderId}, shipment {ShipmentId} by buyer {BuyerId}",
            RequestType, result.CaseNumber, orderId, shipmentId, buyerId);

        // Set success message with case number
        TempData["Success"] = $"Your {(RequestType == "Return" ? "return request" : "complaint")} has been submitted successfully. " +
                             $"Your case number is <strong>{result.CaseNumber}</strong>. The seller will review your request.";
        TempData["CaseNumber"] = result.CaseNumber;
        return RedirectToPage("/Buyer/OrderDetails", new { orderId });
    }

    private Guid? GetBuyerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var buyerId))
        {
            return buyerId;
        }
        return null;
    }
}
