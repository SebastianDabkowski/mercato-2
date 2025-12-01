using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class SettlementDetailsModel : PageModel
    {
        private readonly ILogger<SettlementDetailsModel> _logger;
        private readonly SettlementService _settlementService;
        private readonly IAuditLoggingService _auditLoggingService;
        private readonly IAuthorizationService _authorizationService;

        public SettlementDetailsViewModel? Settlement { get; private set; }
        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        [BindProperty]
        public AddAdjustmentInput AdjustmentInput { get; set; } = new();

        [BindProperty]
        public string? Notes { get; set; }

        public SettlementDetailsModel(
            ILogger<SettlementDetailsModel> logger,
            SettlementService settlementService,
            IAuditLoggingService auditLoggingService,
            IAuthorizationService authorizationService)
        {
            _logger = logger;
            _settlementService = settlementService;
            _auditLoggingService = auditLoggingService;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id, string? success = null, string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            await LoadSettlementAsync(id);

            if (Settlement is null)
            {
                return RedirectToPage("/Admin/Settlements", new { error = "Settlement not found." });
            }

            Notes = Settlement.Notes;

            _logger.LogInformation(
                "Admin {UserId} viewed settlement {SettlementId}: {SettlementNumber}",
                GetUserId(),
                id,
                Settlement.SettlementNumber);

            // Log sensitive data access for audit compliance
            await LogSensitiveAccessAsync(id, Settlement.SellerId, SensitiveAccessAction.View);

            return Page();
        }

        public async Task<IActionResult> OnPostFinalizeAsync(Guid id)
        {
            var command = new FinalizeSettlementCommand(id);
            var result = await _settlementService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} finalized settlement {SettlementId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = "Settlement finalized successfully." });
            }

            return RedirectToPage(new { id, error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id)
        {
            var approvedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin";
            var command = new ApproveSettlementCommand(id, approvedBy);
            var result = await _settlementService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} approved settlement {SettlementId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = "Settlement approved successfully." });
            }

            return RedirectToPage(new { id, error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnPostAddAdjustmentAsync(Guid id)
        {
            if (!ModelState.IsValid)
            {
                await LoadSettlementAsync(id);
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new AddSettlementAdjustmentCommand(
                id,
                AdjustmentInput.OriginalYear,
                AdjustmentInput.OriginalMonth,
                AdjustmentInput.Amount,
                AdjustmentInput.Reason!,
                null,
                AdjustmentInput.RelatedOrderNumber);

            var result = await _settlementService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} added adjustment to settlement {SettlementId}: {Amount} for {Year}-{Month:D2}",
                    GetUserId(),
                    id,
                    AdjustmentInput.Amount,
                    AdjustmentInput.OriginalYear,
                    AdjustmentInput.OriginalMonth);

                return RedirectToPage(new { id, success = "Adjustment added successfully." });
            }

            return RedirectToPage(new { id, error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnPostUpdateNotesAsync(Guid id)
        {
            var command = new UpdateSettlementNotesCommand(id, Notes);
            var success = await _settlementService.HandleAsync(command);

            if (success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated notes for settlement {SettlementId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = "Notes updated successfully." });
            }

            return RedirectToPage(new { id, error = "Failed to update notes." });
        }

        public async Task<IActionResult> OnGetExportAsync(Guid id)
        {
            var settlement = await _settlementService.HandleAsync(new GetSettlementDetailsQuery(id));
            if (settlement is null)
            {
                return RedirectToPage(new { id, error = "Settlement not found." });
            }

            // Mark as exported
            await _settlementService.HandleAsync(new ExportSettlementCommand(id));

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
                id);

            // Log sensitive data export for audit compliance
            await LogSensitiveAccessAsync(id, settlement.SellerId, SensitiveAccessAction.Export);

            var fileName = $"settlement-{settlement.SettlementNumber}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", fileName);
        }

        private async Task LoadSettlementAsync(Guid id)
        {
            var settlement = await _settlementService.HandleAsync(new GetSettlementDetailsQuery(id));

            if (settlement is not null)
            {
                Settlement = new SettlementDetailsViewModel(
                    settlement.Id,
                    settlement.StoreId,
                    settlement.SellerId,
                    settlement.StoreName,
                    settlement.Year,
                    settlement.Month,
                    settlement.SettlementNumber,
                    settlement.Status,
                    settlement.Currency,
                    settlement.GrossSales,
                    settlement.TotalShipping,
                    settlement.TotalCommission,
                    settlement.TotalRefunds,
                    settlement.TotalAdjustments,
                    settlement.NetPayable,
                    settlement.OrderCount,
                    settlement.Version,
                    settlement.PeriodStart,
                    settlement.PeriodEnd,
                    settlement.Items.Select(i => new SettlementItemViewModel(
                        i.Id,
                        i.EscrowAllocationId,
                        i.ShipmentId,
                        i.OrderNumber,
                        i.SellerAmount,
                        i.ShippingAmount,
                        i.CommissionAmount,
                        i.RefundedAmount,
                        i.NetAmount,
                        i.TransactionDate)).ToList(),
                    settlement.Adjustments.Select(a => new SettlementAdjustmentViewModel(
                        a.Id,
                        a.OriginalYear,
                        a.OriginalMonth,
                        a.Amount,
                        a.Reason,
                        a.RelatedOrderId,
                        a.RelatedOrderNumber,
                        a.CreatedAt)).ToList(),
                    settlement.CreatedAt,
                    settlement.FinalizedAt,
                    settlement.ApprovedAt,
                    settlement.ExportedAt,
                    settlement.ApprovedBy,
                    settlement.Notes);
            }
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        private Guid GetUserIdGuid()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private UserRole GetUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            // For admin pages, if role claim is missing or invalid, default to the most restrictive role
            // This ensures audit logging will still capture the access attempt correctly
            if (string.IsNullOrEmpty(roleClaim) || !Enum.TryParse<UserRole>(roleClaim, out var role))
            {
                _logger.LogWarning("Invalid or missing role claim for user {UserId}", GetUserId());
                return UserRole.Buyer; // Most restrictive - will still be logged but won't have elevated access
            }
            return role;
        }

        private async Task LogSensitiveAccessAsync(Guid settlementId, Guid sellerId, SensitiveAccessAction action)
        {
            var userId = GetUserIdGuid();
            var userRole = GetUserRole();

            // Check if audit logging is required for this user role accessing settlement details
            if (_authorizationService.RequiresAuditLogging(userRole, SensitiveResourceType.SettlementDetails))
            {
                await _auditLoggingService.LogSensitiveAccessAsync(
                    userId,
                    userRole,
                    SensitiveResourceType.SettlementDetails,
                    settlementId,
                    action,
                    sellerId, // Resource owner is the seller
                    null,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString());
            }
        }

        public class AddAdjustmentInput
        {
            [Required(ErrorMessage = "Original year is required")]
            [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100")]
            public int OriginalYear { get; set; } = DateTime.UtcNow.Year;

            [Required(ErrorMessage = "Original month is required")]
            [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
            public int OriginalMonth { get; set; } = DateTime.UtcNow.Month == 1 ? 12 : DateTime.UtcNow.Month - 1;

            [Required(ErrorMessage = "Amount is required")]
            public decimal Amount { get; set; }

            [Required(ErrorMessage = "Reason is required")]
            [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters")]
            public string? Reason { get; set; }

            public string? RelatedOrderNumber { get; set; }
        }
    }
}
