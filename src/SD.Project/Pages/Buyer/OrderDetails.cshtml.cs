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

    public OrderDetailsViewModel? Order { get; private set; }

    public OrderDetailsModel(
        ILogger<OrderDetailsModel> logger,
        OrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
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
                s.EstimatedDelivery)).ToList().AsReadOnly());

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
