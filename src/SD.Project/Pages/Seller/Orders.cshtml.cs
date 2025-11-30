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
/// Page model for displaying seller's orders (sub-orders) with filtering and export.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class OrdersModel : PageModel
{
    private readonly ILogger<OrdersModel> _logger;
    private readonly OrderService _orderService;
    private readonly OrderExportService _orderExportService;
    private readonly StoreService _storeService;

    public IReadOnlyList<SellerSubOrderListViewModel> SubOrders { get; private set; } = [];
    public string? StoreName { get; private set; }
    public Guid? StoreId { get; private set; }

    // Pagination
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    // Pagination helper properties for display
    public int DisplayStartItem => (CurrentPage - 1) * PageSize + 1;
    public int DisplayEndItem => Math.Min(CurrentPage * PageSize, TotalCount);

    // Available statuses for filtering
    public IReadOnlyList<string> AvailableStatuses { get; } = new[]
    {
        "Pending", "Paid", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"
    };

    // Filter properties bound from query string
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuyerSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public OrdersModel(
        ILogger<OrdersModel> logger,
        OrderService orderService,
        OrderExportService orderExportService,
        StoreService storeService)
    {
        _logger = logger;
        _orderService = orderService;
        _orderExportService = orderExportService;
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
        CurrentPage = Math.Max(1, PageNumber);

        // Get filtered seller's sub-orders
        var result = await _orderService.HandleAsync(
            new GetFilteredSellerSubOrdersQuery(
                store.Id,
                Status,
                FromDate,
                ToDate,
                BuyerSearch,
                CurrentPage,
                PageSize),
            cancellationToken);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;

        SubOrders = result.Items.Select(s => new SellerSubOrderListViewModel(
            s.SubOrderId,
            s.OrderId,
            s.OrderNumber,
            s.Status,
            s.ItemCount,
            s.Total,
            s.Currency,
            s.BuyerName,
            s.CreatedAt)).ToList().AsReadOnly();

        _logger.LogInformation("Seller orders page accessed for store {StoreId} with {OrderCount} orders (filtered: status={Status}, fromDate={FromDate}, toDate={ToDate}, buyer={Buyer})",
            store.Id, SubOrders.Count, Status, FromDate, ToDate, BuyerSearch);

        return Page();
    }

    public async Task<IActionResult> OnPostExportAsync(string format, CancellationToken cancellationToken = default)
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
            _logger.LogWarning("Store not found for seller {SellerId} during export", userId);
            return RedirectToPage();
        }

        // Parse export format
        if (!Enum.TryParse<ExportFormat>(format, ignoreCase: true, out var exportFormat))
        {
            exportFormat = ExportFormat.Csv;
        }

        var query = new ExportSellerSubOrdersQuery(
            store.Id,
            exportFormat,
            Status,
            FromDate,
            ToDate,
            BuyerSearch);

        var result = await _orderExportService.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess || result.FileData is null || result.ContentType is null || result.FileName is null)
        {
            _logger.LogWarning("Export failed for store {StoreId}: {Errors}",
                store.Id, string.Join(", ", result.Errors));
            TempData["ExportError"] = result.IsSuccess
                ? "Export failed: No data generated."
                : string.Join(", ", result.Errors);
            return RedirectToPage(new { Status, FromDate, ToDate, BuyerSearch, PageNumber });
        }

        _logger.LogInformation(
            "Seller {UserId} exported {OrderCount} orders in {Format} format",
            userId,
            result.ExportedCount,
            exportFormat);

        return File(result.FileData, result.ContentType, result.FileName);
    }
}
