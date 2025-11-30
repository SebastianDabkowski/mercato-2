using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class SettlementsModel : PageModel
    {
        private readonly ILogger<SettlementsModel> _logger;
        private readonly SettlementService _settlementService;
        private readonly StoreService _storeService;

        public IReadOnlyCollection<SettlementListItemViewModel> Settlements { get; private set; } = Array.Empty<SettlementListItemViewModel>();
        public SettlementsSummaryViewModel? Summary { get; private set; }
        public IReadOnlyCollection<SelectListItem> StoreOptions { get; private set; } = Array.Empty<SelectListItem>();
        public IReadOnlyCollection<SelectListItem> YearOptions { get; private set; } = Array.Empty<SelectListItem>();
        public IReadOnlyCollection<SelectListItem> MonthOptions { get; private set; } = Array.Empty<SelectListItem>();
        public IReadOnlyCollection<SelectListItem> StatusOptions { get; private set; } = Array.Empty<SelectListItem>();

        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;
        public int TotalCount { get; private set; }
        public int PageSize { get; private set; } = 20;

        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        // Filter parameters
        [BindProperty(SupportsGet = true)]
        public Guid? StoreId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Month { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public new int Page { get; set; } = 1;

        // Generate settlement input
        [BindProperty]
        public GenerateSettlementInput GenerateInput { get; set; } = new();

        public SettlementsModel(
            ILogger<SettlementsModel> logger,
            SettlementService settlementService,
            StoreService storeService)
        {
            _logger = logger;
            _settlementService = settlementService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            await LoadFiltersAsync();
            await LoadSettlementsAsync();
            await LoadSummaryAsync();

            _logger.LogInformation(
                "Admin {UserId} viewed settlements, found {SettlementCount} settlements",
                GetUserId(),
                TotalCount);

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadFiltersAsync();
                await LoadSettlementsAsync();
                await LoadSummaryAsync();
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            if (GenerateInput.GenerateAll)
            {
                // Generate for all stores
                var command = new GenerateAllSettlementsCommand(
                    GenerateInput.Year,
                    GenerateInput.Month,
                    GenerateInput.Regenerate);

                var results = await _settlementService.HandleAsync(command);
                var successCount = results.Count(r => r.IsSuccess);
                var failureCount = results.Count(r => !r.IsSuccess);

                _logger.LogInformation(
                    "Admin {UserId} generated settlements for all stores: {Year}-{Month:D2}, {SuccessCount} succeeded, {FailureCount} failed",
                    GetUserId(),
                    GenerateInput.Year,
                    GenerateInput.Month,
                    successCount,
                    failureCount);

                if (failureCount == 0)
                {
                    return RedirectToPage(new
                    {
                        success = $"Generated {successCount} settlements for {GenerateInput.Year}-{GenerateInput.Month:D2}.",
                        Year = GenerateInput.Year,
                        Month = GenerateInput.Month
                    });
                }
                else
                {
                    return RedirectToPage(new
                    {
                        success = $"Generated {successCount} settlements. {failureCount} stores had no activity or already have settlements.",
                        Year = GenerateInput.Year,
                        Month = GenerateInput.Month
                    });
                }
            }
            else if (GenerateInput.StoreId.HasValue)
            {
                // Generate for specific store
                var command = new GenerateSettlementCommand(
                    GenerateInput.StoreId.Value,
                    GenerateInput.Year,
                    GenerateInput.Month,
                    GenerateInput.Regenerate);

                var result = await _settlementService.HandleAsync(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Admin {UserId} generated settlement {SettlementNumber} for store {StoreId}: {Year}-{Month:D2}",
                        GetUserId(),
                        result.SettlementNumber,
                        GenerateInput.StoreId.Value,
                        GenerateInput.Year,
                        GenerateInput.Month);

                    return RedirectToPage(new
                    {
                        success = $"Settlement {result.SettlementNumber} generated successfully. Net payable: {result.NetPayable:N2}",
                        StoreId = GenerateInput.StoreId.Value,
                        Year = GenerateInput.Year,
                        Month = GenerateInput.Month
                    });
                }

                await LoadFiltersAsync();
                await LoadSettlementsAsync();
                await LoadSummaryAsync();
                ErrorMessage = result.ErrorMessage;
                return Page();
            }

            await LoadFiltersAsync();
            await LoadSettlementsAsync();
            await LoadSummaryAsync();
            ErrorMessage = "Please select a store or choose to generate for all stores.";
            return Page();
        }

        public async Task<IActionResult> OnPostFinalizeAsync(Guid settlementId)
        {
            var command = new FinalizeSettlementCommand(settlementId);
            var result = await _settlementService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} finalized settlement {SettlementId}",
                    GetUserId(),
                    settlementId);

                return RedirectToPage(new { success = "Settlement finalized successfully." });
            }

            return RedirectToPage(new { error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid settlementId)
        {
            var approvedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin";
            var command = new ApproveSettlementCommand(settlementId, approvedBy);
            var result = await _settlementService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} approved settlement {SettlementId}",
                    GetUserId(),
                    settlementId);

                return RedirectToPage(new { success = "Settlement approved successfully." });
            }

            return RedirectToPage(new { error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnGetExportAsync(Guid settlementId)
        {
            var settlement = await _settlementService.HandleAsync(new GetSettlementDetailsQuery(settlementId));
            if (settlement is null)
            {
                return RedirectToPage(new { error = "Settlement not found." });
            }

            // Mark as exported
            await _settlementService.HandleAsync(new ExportSettlementCommand(settlementId));

            // Generate CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Settlement Number,Store Name,Period,Currency,Gross Sales,Total Shipping,Total Commission,Total Refunds,Total Adjustments,Net Payable,Order Count,Status,Generated At");
            csv.AppendLine($"\"{settlement.SettlementNumber}\",\"{settlement.StoreName}\",\"{settlement.Year}-{settlement.Month:D2}\",\"{settlement.Currency}\",{settlement.GrossSales:F2},{settlement.TotalShipping:F2},{settlement.TotalCommission:F2},{settlement.TotalRefunds:F2},{settlement.TotalAdjustments:F2},{settlement.NetPayable:F2},{settlement.OrderCount},\"{settlement.Status}\",\"{settlement.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");

            if (settlement.Items.Count > 0)
            {
                csv.AppendLine();
                csv.AppendLine("Order Details");
                csv.AppendLine("Order Number,Seller Amount,Shipping Amount,Commission Amount,Refunded Amount,Net Amount,Transaction Date");
                foreach (var item in settlement.Items)
                {
                    csv.AppendLine($"\"{item.OrderNumber ?? "N/A"}\",{item.SellerAmount:F2},{item.ShippingAmount:F2},{item.CommissionAmount:F2},{item.RefundedAmount:F2},{item.NetAmount:F2},\"{item.TransactionDate:yyyy-MM-dd}\"");
                }
            }

            if (settlement.Adjustments.Count > 0)
            {
                csv.AppendLine();
                csv.AppendLine("Adjustments");
                csv.AppendLine("Original Period,Amount,Reason,Related Order,Created At");
                foreach (var adj in settlement.Adjustments)
                {
                    csv.AppendLine($"\"{adj.OriginalYear}-{adj.OriginalMonth:D2}\",{adj.Amount:F2},\"{adj.Reason}\",\"{adj.RelatedOrderNumber ?? "N/A"}\",\"{adj.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }
            }

            _logger.LogInformation(
                "Admin {UserId} exported settlement {SettlementId}",
                GetUserId(),
                settlementId);

            var fileName = $"settlement-{settlement.SettlementNumber}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", fileName);
        }

        private async Task LoadSettlementsAsync()
        {
            SettlementStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(Status) && Enum.TryParse<SettlementStatus>(Status, true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var query = new GetSettlementsQuery(
                StoreId,
                Year,
                Month,
                statusFilter,
                Page,
                PageSize);

            var result = await _settlementService.HandleAsync(query);

            Settlements = result.Items.Select(s => new SettlementListItemViewModel(
                s.Id,
                s.StoreId,
                s.StoreName,
                s.Year,
                s.Month,
                s.SettlementNumber,
                s.Status,
                s.Currency,
                s.NetPayable,
                s.OrderCount,
                s.Version,
                s.CreatedAt,
                s.FinalizedAt)).ToList();

            CurrentPage = result.PageNumber;
            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
        }

        private async Task LoadSummaryAsync()
        {
            var query = new GetSettlementsSummaryQuery(Year, Month);
            var summary = await _settlementService.HandleAsync(query);

            Summary = new SettlementsSummaryViewModel(
                summary.TotalSettlements,
                summary.DraftCount,
                summary.FinalizedCount,
                summary.ApprovedCount,
                summary.ExportedCount,
                summary.TotalNetPayable,
                summary.Currency);
        }

        private async Task LoadFiltersAsync()
        {
            // Load stores
            var stores = await _storeService.HandleAsync(new GetPublicStoresQuery());
            var storeList = new List<SelectListItem>
            {
                new SelectListItem("All Stores", "")
            };
            storeList.AddRange(stores.Select(s => new SelectListItem(s.Name, s.Id.ToString())));
            StoreOptions = storeList;

            // Year options
            var currentYear = DateTime.UtcNow.Year;
            var yearList = new List<SelectListItem>
            {
                new SelectListItem("All Years", "")
            };
            for (var y = currentYear; y >= currentYear - 3; y--)
            {
                yearList.Add(new SelectListItem(y.ToString(), y.ToString()));
            }
            YearOptions = yearList;

            // Month options
            var monthList = new List<SelectListItem>
            {
                new SelectListItem("All Months", "")
            };
            for (var m = 1; m <= 12; m++)
            {
                var monthName = new DateTime(2000, m, 1).ToString("MMMM");
                monthList.Add(new SelectListItem(monthName, m.ToString()));
            }
            MonthOptions = monthList;

            // Status options
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem("All Statuses", ""),
                new SelectListItem("Draft", "Draft"),
                new SelectListItem("Finalized", "Finalized"),
                new SelectListItem("Approved", "Approved"),
                new SelectListItem("Exported", "Exported")
            };
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        public class GenerateSettlementInput
        {
            public Guid? StoreId { get; set; }

            [Required(ErrorMessage = "Year is required")]
            [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100")]
            public int Year { get; set; } = DateTime.UtcNow.Year;

            [Required(ErrorMessage = "Month is required")]
            [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
            public int Month { get; set; } = DateTime.UtcNow.Month == 1 ? 12 : DateTime.UtcNow.Month - 1;

            public bool GenerateAll { get; set; }

            public bool Regenerate { get; set; }
        }
    }
}
