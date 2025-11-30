using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying buyer's orders with filtering and pagination.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Admin)]
public class OrdersModel : PageModel
{
    private readonly ILogger<OrdersModel> _logger;
    private readonly CheckoutService _checkoutService;

    public IReadOnlyList<BuyerOrderListItemViewModel> Orders { get; private set; } = [];

    // Pagination
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    // Pagination helper properties for display
    public int DisplayStartItem => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int DisplayEndItem => Math.Min(CurrentPage * PageSize, TotalCount);

    // Available statuses for filtering
    public IReadOnlyList<string> AvailableStatuses { get; } = new[]
    {
        "Pending", "PaymentConfirmed", "Processing", "Shipped", "Delivered", "Cancelled", "PaymentFailed", "Refunded"
    };

    // Filter properties bound from query string
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public OrdersModel(
        ILogger<OrdersModel> logger,
        CheckoutService checkoutService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var buyerId))
        {
            return RedirectToPage("/Login");
        }

        CurrentPage = Math.Max(1, PageNumber);

        // Get filtered buyer's orders
        var result = await _checkoutService.HandleAsync(
            new GetFilteredBuyerOrdersQuery(
                buyerId,
                Status,
                FromDate,
                ToDate,
                null, // SellerId filter - optional
                CurrentPage,
                PageSize),
            cancellationToken);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;

        Orders = result.Items.Select(o => new BuyerOrderListItemViewModel(
            o.OrderId,
            o.OrderNumber,
            o.Status,
            o.ItemCount,
            o.TotalAmount,
            o.Currency,
            o.CreatedAt)).ToList().AsReadOnly();

        _logger.LogInformation("Buyer orders page accessed by {BuyerId} with {OrderCount} orders (filtered: status={Status}, fromDate={FromDate}, toDate={ToDate})",
            buyerId, Orders.Count, Status, FromDate, ToDate);

        return Page();
    }
}
