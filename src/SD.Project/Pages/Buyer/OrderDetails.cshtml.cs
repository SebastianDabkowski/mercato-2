using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying order details with seller sub-order breakdown.
/// </summary>
public class OrderDetailsModel : PageModel
{
    private readonly ILogger<OrderDetailsModel> _logger;
    private readonly OrderService _orderService;
    private readonly ReturnRequestService _returnRequestService;

    public OrderDetailsViewModel? Order { get; private set; }
    
    /// <summary>
    /// Dictionary of sub-order ID to its return eligibility status.
    /// </summary>
    public Dictionary<Guid, ReturnEligibilityViewModel> ReturnEligibility { get; private set; } = new();

    /// <summary>
    /// Dictionary of sub-order ID to its existing return request (if any).
    /// </summary>
    public Dictionary<Guid, BuyerReturnRequestViewModel> ExistingReturnRequests { get; private set; } = new();

    public OrderDetailsModel(
        ILogger<OrderDetailsModel> logger,
        OrderService orderService,
        ReturnRequestService returnRequestService)
    {
        _logger = logger;
        _orderService = orderService;
        _returnRequestService = returnRequestService;
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Verify user is authenticated
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login", new { returnUrl = $"/Buyer/OrderDetails/{orderId}" });
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login");
        }

        // Get order details with seller sub-order breakdown
        var orderDetails = await _orderService.HandleAsync(
            new GetBuyerOrderDetailsQuery(buyerId.Value, orderId),
            cancellationToken);

        if (orderDetails is null)
        {
            _logger.LogWarning("Order not found or access denied: {OrderId} for buyer {BuyerId}", orderId, buyerId);
            return Page();
        }

        // Map DTO to ViewModel
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

        // Check return eligibility and existing return requests for each sub-order
        foreach (var subOrder in Order.SellerSubOrders)
        {
            // Check eligibility
            var eligibility = await _returnRequestService.HandleAsync(
                new CheckReturnEligibilityQuery(buyerId.Value, orderId, subOrder.SubOrderId),
                cancellationToken);

            ReturnEligibility[subOrder.SubOrderId] = new ReturnEligibilityViewModel(
                eligibility.IsEligible,
                eligibility.IneligibilityReason,
                eligibility.ReturnWindowEndsAt,
                eligibility.HasExistingReturnRequest,
                eligibility.ExistingReturnStatus);

            // Get existing return request if any
            var existingReturn = await _returnRequestService.HandleAsync(
                new GetReturnRequestByShipmentQuery(buyerId.Value, subOrder.SubOrderId),
                cancellationToken);

            if (existingReturn is not null)
            {
                var items = existingReturn.Items.Select(i => new ReturnRequestItemViewModel(
                    i.ItemId,
                    i.OrderItemId,
                    i.ProductName,
                    i.Quantity)).ToList();

                ExistingReturnRequests[subOrder.SubOrderId] = new BuyerReturnRequestViewModel(
                    existingReturn.ReturnRequestId,
                    existingReturn.OrderId,
                    existingReturn.ShipmentId,
                    existingReturn.CaseNumber,
                    existingReturn.OrderNumber,
                    existingReturn.StoreName,
                    existingReturn.Type,
                    existingReturn.Status,
                    existingReturn.Reason,
                    existingReturn.Comments,
                    existingReturn.SellerResponse,
                    existingReturn.CreatedAt,
                    existingReturn.ApprovedAt,
                    existingReturn.RejectedAt,
                    existingReturn.CompletedAt,
                    items.AsReadOnly());
            }
        }

        _logger.LogInformation("Order details viewed: {OrderNumber} with {SubOrderCount} seller sub-orders",
            Order.OrderNumber, Order.SellerSubOrders.Count);

        return Page();
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
