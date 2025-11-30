using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for displaying seller's sub-order details.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class OrderDetailModel : PageModel
{
    private readonly ILogger<OrderDetailModel> _logger;
    private readonly OrderService _orderService;
    private readonly StoreService _storeService;

    public SellerSubOrderDetailsViewModel? SubOrder { get; private set; }
    public string? StoreName { get; private set; }

    public OrderDetailModel(
        ILogger<OrderDetailModel> logger,
        OrderService orderService,
        StoreService storeService)
    {
        _logger = logger;
        _orderService = orderService;
        _storeService = storeService;
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

        _logger.LogInformation("Seller order detail page accessed for order {OrderNumber}, sub-order {SubOrderId}",
            SubOrder.OrderNumber, SubOrder.SubOrderId);

        return Page();
    }
}
