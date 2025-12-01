using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class SalesDashboardModel : PageModel
    {
        private readonly ILogger<SalesDashboardModel> _logger;
        private readonly SellerSalesDashboardService _dashboardService;
        private readonly StoreService _storeService;

        public SalesDashboardModel(
            ILogger<SalesDashboardModel> logger,
            SellerSalesDashboardService dashboardService,
            StoreService storeService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
            _storeService = storeService;
        }

        public SellerSalesDashboardViewModel Dashboard { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? DateRange { get; set; } = "last7days";

        [BindProperty(SupportsGet = true)]
        public DateTime? CustomFromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? CustomToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Granularity { get; set; } = "day";

        [BindProperty(SupportsGet = true)]
        public Guid? ProductId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        public IReadOnlyList<DateRangePreset> DateRangePresets { get; private set; } = Array.Empty<DateRangePreset>();
        public IReadOnlyList<GranularityOption> GranularityOptions { get; private set; } = Array.Empty<GranularityOption>();
        public IReadOnlyList<ProductFilterOption> ProductOptions { get; private set; } = Array.Empty<ProductFilterOption>();
        public IReadOnlyList<string> CategoryOptions { get; private set; } = Array.Empty<string>();

        public bool HasStore { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Seller sales dashboard accessed by user {UserId}", userIdClaim);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return RedirectToPage("/Login");
            }

            // Get the seller's store
            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                HasStore = false;
                return Page();
            }

            HasStore = true;

            // Set date range presets
            DateRangePresets = new List<DateRangePreset>
            {
                new() { Name = "Today", Value = "today", IsSelected = DateRange == "today" },
                new() { Name = "Last 7 Days", Value = "last7days", IsSelected = DateRange == "last7days" },
                new() { Name = "Last 30 Days", Value = "last30days", IsSelected = DateRange == "last30days" },
                new() { Name = "Custom Range", Value = "custom", IsSelected = DateRange == "custom" }
            };

            // Set granularity options
            GranularityOptions = new List<GranularityOption>
            {
                new() { Name = "Daily", Value = "day", IsSelected = Granularity == "day" },
                new() { Name = "Weekly", Value = "week", IsSelected = Granularity == "week" },
                new() { Name = "Monthly", Value = "month", IsSelected = Granularity == "month" }
            };

            // Calculate date range
            var (fromDate, toDate) = CalculateDateRange();

            // Get filter options
            var filterOptions = await _dashboardService.HandleAsync(
                new GetSellerDashboardFilterOptionsQuery(store.Id));

            ProductOptions = filterOptions.Products
                .Select(p => new ProductFilterOption
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    IsSelected = ProductId == p.ProductId
                })
                .ToList();

            CategoryOptions = filterOptions.Categories;

            // Get dashboard data
            var query = new GetSellerSalesDashboardQuery(
                store.Id,
                fromDate,
                toDate,
                Granularity ?? "day",
                ProductId,
                Category);

            var dashboardData = await _dashboardService.HandleAsync(query);

            // Map to view model
            Dashboard = new SellerSalesDashboardViewModel
            {
                Gmv = dashboardData.Gmv,
                OrderCount = dashboardData.OrderCount,
                ItemCount = dashboardData.ItemCount,
                AverageOrderValue = dashboardData.AverageOrderValue,
                Currency = dashboardData.Currency,
                FromDate = dashboardData.FromDate,
                ToDate = dashboardData.ToDate,
                HasData = dashboardData.HasData,
                RefreshedAt = dashboardData.RefreshedAt,
                Granularity = dashboardData.Granularity,
                TimeSeries = dashboardData.TimeSeries
                    .Select(dp => new SellerSalesDataPointViewModel
                    {
                        PeriodStart = dp.PeriodStart,
                        PeriodLabel = dp.PeriodLabel,
                        Gmv = dp.Gmv,
                        OrderCount = dp.OrderCount
                    })
                    .ToList()
            };

            return Page();
        }

        private (DateTime FromDate, DateTime ToDate) CalculateDateRange()
        {
            var today = DateTime.UtcNow.Date;

            return DateRange switch
            {
                "today" => (today, today),
                "last7days" => (today.AddDays(-6), today),
                "last30days" => (today.AddDays(-29), today),
                "custom" => (
                    CustomFromDate?.Date ?? today.AddDays(-6),
                    CustomToDate?.Date ?? today
                ),
                _ => (today.AddDays(-6), today)
            };
        }
    }
}
