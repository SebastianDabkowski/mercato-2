using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class OrderRevenueReportModel : PageModel
    {
        private readonly ILogger<OrderRevenueReportModel> _logger;
        private readonly AdminOrderReportService _reportService;
        private readonly IStoreRepository _storeRepository;

        public OrderRevenueReportModel(
            ILogger<OrderRevenueReportModel> logger,
            AdminOrderReportService reportService,
            IStoreRepository storeRepository)
        {
            _logger = logger;
            _reportService = reportService;
            _storeRepository = storeRepository;
        }

        public OrderReportViewModel Report { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? DateRange { get; set; } = "last30days";

        [BindProperty(SupportsGet = true)]
        public DateTime? CustomFromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? CustomToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? SellerId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OrderStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PaymentStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public IReadOnlyList<DateRangePreset> DateRangePresets { get; private set; } = Array.Empty<DateRangePreset>();
        public IReadOnlyList<SellerFilterOption> SellerOptions { get; private set; } = Array.Empty<SellerFilterOption>();
        public IReadOnlyList<string> OrderStatusOptions { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> PaymentStatusOptions { get; private set; } = Array.Empty<string>();

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Admin order/revenue report accessed by user {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Set filter options
            DateRangePresets = new List<DateRangePreset>
            {
                new() { Name = "Last 7 Days", Value = "last7days", IsSelected = DateRange == "last7days" },
                new() { Name = "Last 30 Days", Value = "last30days", IsSelected = DateRange == "last30days" },
                new() { Name = "Last 90 Days", Value = "last90days", IsSelected = DateRange == "last90days" },
                new() { Name = "Custom Range", Value = "custom", IsSelected = DateRange == "custom" }
            };

            OrderStatusOptions = Enum.GetNames<OrderStatus>().ToList();
            PaymentStatusOptions = new List<string> { "Pending", "Paid", "Failed", "Refunded" };

            // Load seller options
            var stores = await _storeRepository.GetPubliclyVisibleAsync();
            SellerOptions = stores
                .OrderBy(s => s.Name)
                .Select(s => new SellerFilterOption
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsSelected = SellerId == s.Id
                })
                .ToList();

            // Calculate date range
            var (fromDate, toDate) = CalculateDateRange();

            // Get report data
            var query = new GetAdminOrderReportQuery(
                fromDate,
                toDate,
                SellerId,
                OrderStatus,
                PaymentStatus,
                PageNumber,
                20);

            var result = await _reportService.HandleAsync(query);

            // Map to view model
            Report = new OrderReportViewModel
            {
                Rows = result.Rows.Select(r => new OrderReportRowViewModel
                {
                    OrderId = r.OrderId,
                    OrderNumber = r.OrderNumber,
                    OrderDate = r.OrderDate,
                    BuyerName = r.BuyerName,
                    SellerName = r.SellerName,
                    OrderStatus = r.OrderStatus,
                    PaymentStatus = r.PaymentStatus,
                    OrderValue = r.OrderValue,
                    Commission = r.Commission,
                    PayoutAmount = r.PayoutAmount,
                    Currency = r.Currency
                }).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalOrderValue = result.TotalOrderValue,
                TotalCommission = result.TotalCommission,
                TotalPayoutAmount = result.TotalPayoutAmount,
                Currency = result.Currency,
                ExceedsExportThreshold = result.ExceedsExportThreshold
            };
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            _logger.LogInformation("Admin CSV export requested by user {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var (fromDate, toDate) = CalculateDateRange();

            var query = new ExportAdminOrderReportQuery(
                fromDate,
                toDate,
                SellerId,
                OrderStatus,
                PaymentStatus);

            var result = await _reportService.HandleAsync(query);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Export failed.";
                return RedirectToPage();
            }

            return File(result.FileData!, result.ContentType!, result.FileName);
        }

        private (DateTime FromDate, DateTime ToDate) CalculateDateRange()
        {
            var today = DateTime.UtcNow.Date;

            return DateRange switch
            {
                "last7days" => (today.AddDays(-6), today),
                "last30days" => (today.AddDays(-29), today),
                "last90days" => (today.AddDays(-89), today),
                "custom" => (
                    CustomFromDate?.Date ?? today.AddDays(-29),
                    CustomToDate?.Date ?? today
                ),
                _ => (today.AddDays(-29), today)
            };
        }
    }
}
