using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Admin page for viewing order details with full shipping status history.
/// </summary>
[RequireRole(UserRole.Admin)]
public class OrderDetailsModel : PageModel
{
    private readonly ILogger<OrderDetailsModel> _logger;
    private readonly OrderService _orderService;

    public AdminOrderDetailsViewModel? Order { get; private set; }

    public OrderDetailsModel(
        ILogger<OrderDetailsModel> logger,
        OrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Get admin order details with status history
        var orderDetails = await _orderService.HandleAsync(
            new GetAdminOrderDetailsQuery(orderId),
            cancellationToken);

        if (orderDetails is null)
        {
            _logger.LogWarning("Admin order details not found: {OrderId}", orderId);
            return Page();
        }

        // Map DTO to ViewModel
        Order = new AdminOrderDetailsViewModel(
            orderDetails.OrderId,
            orderDetails.OrderNumber,
            orderDetails.Status,
            orderDetails.PaymentStatus,
            orderDetails.BuyerId,
            orderDetails.BuyerName,
            orderDetails.BuyerEmail,
            orderDetails.RecipientName,
            orderDetails.DeliveryAddressSummary,
            orderDetails.PaymentMethodName,
            orderDetails.PaymentTransactionId,
            orderDetails.ItemSubtotal,
            orderDetails.TotalShipping,
            orderDetails.TotalAmount,
            orderDetails.Currency,
            orderDetails.CreatedAt,
            orderDetails.PaidAt,
            orderDetails.CancelledAt,
            orderDetails.RefundedAt,
            orderDetails.RefundedAmount,
            orderDetails.Shipments.Select(s => new AdminShipmentViewModel(
                s.ShipmentId,
                s.StoreId,
                s.StoreName,
                s.Status,
                s.Subtotal,
                s.ShippingCost,
                s.Total,
                s.CarrierName,
                s.TrackingNumber,
                s.TrackingUrl,
                s.CreatedAt,
                s.ShippedAt,
                s.DeliveredAt,
                s.CancelledAt,
                s.RefundedAt,
                s.RefundedAmount,
                s.Items.Select(i => new AdminOrderItemViewModel(
                    i.ItemId,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.LineTotal,
                    i.ShippingMethodName,
                    i.Status)).ToList().AsReadOnly(),
                s.StatusHistory.Select(h => new ShipmentStatusHistoryViewModel(
                    h.Id,
                    h.ShipmentId,
                    h.PreviousStatus,
                    h.NewStatus,
                    h.ChangedAt,
                    h.ChangedByUserId,
                    h.ChangedByUserName,
                    h.ActorType,
                    h.CarrierName,
                    h.TrackingNumber,
                    h.TrackingUrl,
                    h.Notes)).ToList().AsReadOnly())).ToList().AsReadOnly());

        _logger.LogInformation("Admin order details viewed: {OrderNumber} with {ShipmentCount} shipments",
            Order.OrderNumber, Order.Shipments.Count);

        return Page();
    }
}
