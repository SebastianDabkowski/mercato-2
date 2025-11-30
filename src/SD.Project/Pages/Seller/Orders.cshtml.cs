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
/// Page model for displaying seller's orders (sub-orders).
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class OrdersModel : PageModel
{
    private readonly ILogger<OrdersModel> _logger;
    private readonly OrderService _orderService;
    private readonly StoreService _storeService;

    public IReadOnlyList<SellerSubOrderListViewModel> SubOrders { get; private set; } = [];
    public string? StoreName { get; private set; }
    public Guid? StoreId { get; private set; }

    public OrdersModel(
        ILogger<OrdersModel> logger,
        OrderService orderService,
        StoreService storeService)
    {
        _logger = logger;
        _orderService = orderService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
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

        StoreId = store.Id;
        StoreName = store.Name;

        // Get seller's sub-orders
        var subOrders = await _orderService.HandleAsync(
            new GetSellerSubOrdersQuery(store.Id, 0, 50),
            cancellationToken);

        SubOrders = subOrders.Select(s => new SellerSubOrderListViewModel(
            s.SubOrderId,
            s.OrderId,
            s.OrderNumber,
            s.Status,
            s.ItemCount,
            s.Total,
            s.Currency,
            s.BuyerName,
            s.CreatedAt)).ToList().AsReadOnly();

        _logger.LogInformation("Seller orders page accessed for store {StoreId} with {OrderCount} orders",
            store.Id, SubOrders.Count);

        return Page();
    }
}
