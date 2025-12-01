using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin, UserRole.Support, UserRole.Compliance)]
    public class ReturnsDisputesModel : PageModel
    {
        private readonly ILogger<ReturnsDisputesModel> _logger;
        private readonly ReturnRequestService _returnRequestService;

        public IReadOnlyCollection<AdminReturnRequestSummaryViewModel> Cases { get; private set; } = Array.Empty<AdminReturnRequestSummaryViewModel>();
        public IReadOnlyCollection<SelectListItem> StatusOptions { get; private set; } = Array.Empty<SelectListItem>();
        public IReadOnlyCollection<SelectListItem> TypeOptions { get; private set; } = Array.Empty<SelectListItem>();

        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;
        public int TotalCount { get; private set; }
        public int PageSize { get; private set; } = 20;

        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        // Filter parameters
        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public new int Page { get; set; } = 1;

        public ReturnsDisputesModel(
            ILogger<ReturnsDisputesModel> logger,
            ReturnRequestService returnRequestService)
        {
            _logger = logger;
            _returnRequestService = returnRequestService;
        }

        public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            LoadFilters();
            await LoadCasesAsync();

            _logger.LogInformation(
                "Admin {UserId} viewed returns & disputes, found {CaseCount} cases",
                GetUserId(),
                TotalCount);

            return Page();
        }

        public async Task<IActionResult> OnPostEscalateAsync(Guid caseId, string reason, string? notes)
        {
            if (caseId == Guid.Empty)
            {
                return RedirectToPage(new { error = "Invalid case ID." });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return RedirectToPage(new { error = "Escalation reason is required." });
            }

            var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
            if (userId == Guid.Empty)
            {
                return RedirectToPage(new { error = "Unable to determine admin user." });
            }

            var command = new EscalateCaseCommand(caseId, userId, reason, notes);
            var result = await _returnRequestService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} escalated case {CaseId} with reason {Reason}",
                    GetUserId(),
                    caseId,
                    reason);

                return RedirectToPage(new { success = $"Case escalated successfully. New status: {result.NewStatus}" });
            }

            return RedirectToPage(new { error = result.ErrorMessage });
        }

        private async Task LoadCasesAsync()
        {
            var query = new GetAdminReturnRequestsQuery(
                Status,
                Type,
                SearchTerm,
                FromDate,
                ToDate,
                Page,
                PageSize);

            var result = await _returnRequestService.HandleAsync(query);

            Cases = result.Items.Select(c => new AdminReturnRequestSummaryViewModel(
                c.ReturnRequestId,
                c.OrderId,
                c.StoreId,
                c.CaseNumber,
                c.OrderNumber,
                c.StoreName,
                c.Type,
                c.Status,
                c.SellerName,
                c.BuyerAlias,
                c.Reason,
                c.SubOrderTotal,
                c.Currency,
                c.CreatedAt,
                c.AgeInDays,
                c.IsEscalated,
                c.EscalatedAt)).ToList();

            CurrentPage = result.PageNumber;
            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
        }

        private void LoadFilters()
        {
            // Status options
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem("All Statuses", ""),
                new SelectListItem("Requested", "Requested"),
                new SelectListItem("Approved", "Approved"),
                new SelectListItem("Rejected", "Rejected"),
                new SelectListItem("Under Admin Review", "UnderAdminReview"),
                new SelectListItem("Completed", "Completed")
            };

            // Type options
            TypeOptions = new List<SelectListItem>
            {
                new SelectListItem("All Types", ""),
                new SelectListItem("Return", "Return"),
                new SelectListItem("Complaint", "Complaint")
            };
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }
    }
}
