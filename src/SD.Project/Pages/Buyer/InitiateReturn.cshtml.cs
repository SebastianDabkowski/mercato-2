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
/// Page model for initiating a return request.
/// </summary>
public class InitiateReturnModel : PageModel
{
    private readonly ILogger<InitiateReturnModel> _logger;
    private readonly OrderService _orderService;
    private readonly ReturnRequestService _returnRequestService;

    public OrderDetailsViewModel? Order { get; private set; }
    public SellerSubOrderViewModel? SubOrder { get; private set; }
    public ReturnEligibilityViewModel? Eligibility { get; private set; }
    
    [BindProperty]
    [Required(ErrorMessage = "Please select a reason for the return")]
    public string Reason { get; set; } = string.Empty;

    [BindProperty]
    [MaxLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters")]
    public string? Comments { get; set; }

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

        if (!ModelState.IsValid)
        {
            // Reload page data
            await OnGetAsync(orderId, shipmentId, cancellationToken);
            return Page();
        }

        // Submit return request
        var result = await _returnRequestService.HandleAsync(
            new InitiateReturnRequestCommand(
                buyerId.Value,
                orderId,
                shipmentId,
                Reason,
                Comments),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to submit return request.";
            await OnGetAsync(orderId, shipmentId, cancellationToken);
            return Page();
        }

        _logger.LogInformation("Return request {ReturnRequestId} created for order {OrderId}, shipment {ShipmentId} by buyer {BuyerId}",
            result.ReturnRequestId, orderId, shipmentId, buyerId);

        TempData["Success"] = "Your return request has been submitted successfully. The seller will review your request.";
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
