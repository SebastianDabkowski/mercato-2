using System.ComponentModel.DataAnnotations;
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
    public class CaseDetailsModel : PageModel
    {
        private readonly ILogger<CaseDetailsModel> _logger;
        private readonly ReturnRequestService _returnRequestService;
        private readonly CaseMessageService _caseMessageService;

        public AdminReturnRequestDetailsViewModel? CaseDetails { get; private set; }
        public CaseMessageThreadViewModel? MessageThread { get; private set; }
        public IReadOnlyCollection<SelectListItem> DecisionTypeOptions { get; private set; } = Array.Empty<SelectListItem>();
        public IReadOnlyCollection<SelectListItem> ResolutionTypeOptions { get; private set; } = Array.Empty<SelectListItem>();
        public IReadOnlyCollection<SelectListItem> EscalationReasonOptions { get; private set; } = Array.Empty<SelectListItem>();

        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        [BindProperty]
        public AdminDecisionInput DecisionInput { get; set; } = new();

        [BindProperty]
        public EscalationInput EscalateInput { get; set; } = new();

        [BindProperty]
        public string? NewMessage { get; set; }

        public CaseDetailsModel(
            ILogger<CaseDetailsModel> logger,
            ReturnRequestService returnRequestService,
            CaseMessageService caseMessageService)
        {
            _logger = logger;
            _returnRequestService = returnRequestService;
            _caseMessageService = caseMessageService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id, string? success = null, string? error = null)
        {
            if (id == Guid.Empty)
            {
                return RedirectToPage("/Admin/ReturnsDisputes", new { error = "Invalid case ID." });
            }

            SuccessMessage = success;
            ErrorMessage = error;

            await LoadCaseDetailsAsync(id);

            if (CaseDetails is null)
            {
                return RedirectToPage("/Admin/ReturnsDisputes", new { error = "Case not found." });
            }

            LoadOptions();
            await LoadMessagesAsync(id);

            _logger.LogInformation(
                "Admin {UserId} viewed case {CaseId} ({CaseNumber})",
                GetUserId(),
                id,
                CaseDetails.CaseNumber);

            return Page();
        }

        public async Task<IActionResult> OnPostEscalateAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return RedirectToPage(new { id, error = "Invalid case ID." });
            }

            if (string.IsNullOrWhiteSpace(EscalateInput.Reason))
            {
                return RedirectToPage(new { id, error = "Escalation reason is required." });
            }

            var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
            if (userId == Guid.Empty)
            {
                return RedirectToPage(new { id, error = "Unable to determine admin user." });
            }

            var command = new EscalateCaseCommand(id, userId, EscalateInput.Reason, EscalateInput.Notes);
            var result = await _returnRequestService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} escalated case {CaseId} with reason {Reason}",
                    GetUserId(),
                    id,
                    EscalateInput.Reason);

                return RedirectToPage(new { id, success = $"Case escalated successfully. Status: {result.NewStatus}" });
            }

            return RedirectToPage(new { id, error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnPostRecordDecisionAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return RedirectToPage(new { id, error = "Invalid case ID." });
            }

            if (!ModelState.IsValid)
            {
                await LoadCaseDetailsAsync(id);
                if (CaseDetails is null)
                {
                    return RedirectToPage("/Admin/ReturnsDisputes", new { error = "Case not found." });
                }
                LoadOptions();
                await LoadMessagesAsync(id);
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
            if (userId == Guid.Empty)
            {
                return RedirectToPage(new { id, error = "Unable to determine admin user." });
            }

            var command = new RecordAdminDecisionCommand(
                id,
                userId,
                DecisionInput.DecisionType!,
                DecisionInput.DecisionNotes!,
                DecisionInput.ResolutionType,
                DecisionInput.PartialRefundAmount,
                DecisionInput.InitiateRefund);

            var result = await _returnRequestService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} recorded decision {Decision} for case {CaseId}",
                    GetUserId(),
                    DecisionInput.DecisionType,
                    id);

                var message = $"Decision recorded: {ReturnRequestStatusHelper.GetAdminDecisionDisplayName(result.Decision)}.";
                if (result.RefundId.HasValue)
                {
                    message += $" Refund initiated (Status: {result.RefundStatus}).";
                }

                return RedirectToPage(new { id, success = message });
            }

            return RedirectToPage(new { id, error = result.ErrorMessage });
        }

        public async Task<IActionResult> OnPostSendMessageAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return RedirectToPage(new { id, error = "Invalid case ID." });
            }

            if (string.IsNullOrWhiteSpace(NewMessage))
            {
                return RedirectToPage(new { id, error = "Message content is required." });
            }

            var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
            if (userId == Guid.Empty)
            {
                return RedirectToPage(new { id, error = "Unable to determine admin user." });
            }

            var command = new SendCaseMessageCommand(id, userId, "admin", NewMessage);
            var result = await _caseMessageService.HandleAsync(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Admin {UserId} sent message to case {CaseId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = "Message sent successfully." });
            }

            return RedirectToPage(new { id, error = result.ErrorMessage });
        }

        private async Task LoadCaseDetailsAsync(Guid id)
        {
            var query = new GetAdminReturnRequestDetailsQuery(id);
            var dto = await _returnRequestService.HandleAsync(query);

            if (dto is null)
            {
                CaseDetails = null;
                return;
            }

            CaseDetails = new AdminReturnRequestDetailsViewModel(
                dto.ReturnRequestId,
                dto.OrderId,
                dto.ShipmentId,
                dto.StoreId,
                dto.CaseNumber,
                dto.OrderNumber,
                dto.StoreName,
                dto.Type,
                dto.Status,
                dto.SellerName,
                dto.SellerEmail,
                dto.BuyerName,
                dto.BuyerEmail,
                dto.Reason,
                dto.Comments,
                dto.SellerResponse,
                dto.SubOrderTotal,
                dto.Currency,
                dto.CreatedAt,
                dto.ApprovedAt,
                dto.RejectedAt,
                dto.CompletedAt,
                dto.Items.Select(i => new AdminCaseItemViewModel(
                    i.ItemId,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.LineTotal,
                    i.ShippingMethodName,
                    i.Status)).ToList().AsReadOnly(),
                dto.RequestItems.Select(i => new ReturnRequestItemViewModel(
                    i.ItemId,
                    i.OrderItemId,
                    i.ProductName,
                    i.Quantity)).ToList().AsReadOnly(),
                dto.ResolutionType,
                dto.ResolutionNotes,
                dto.PartialRefundAmount,
                dto.ResolvedAt,
                dto.LinkedRefundId,
                dto.LinkedRefund is not null ? new LinkedRefundViewModel(
                    dto.LinkedRefund.RefundId,
                    dto.LinkedRefund.Status,
                    dto.LinkedRefund.Amount,
                    dto.LinkedRefund.Currency,
                    dto.LinkedRefund.RefundTransactionId,
                    dto.LinkedRefund.CreatedAt,
                    dto.LinkedRefund.CompletedAt) : null,
                dto.IsEscalated,
                dto.EscalatedAt,
                dto.EscalatedByUserId,
                dto.EscalationReason,
                dto.EscalationNotes,
                dto.HasAdminDecision,
                dto.AdminDecisionByUserId,
                dto.AdminDecision,
                dto.AdminDecisionNotes,
                dto.AdminDecisionAt,
                dto.CanEscalate,
                dto.CanRecordDecision);
        }

        private async Task LoadMessagesAsync(Guid caseId)
        {
            var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
            if (userId == Guid.Empty)
            {
                return;
            }

            var query = new GetCaseMessagesQuery(caseId, userId, "admin");
            var dto = await _caseMessageService.HandleAsync(query);

            if (dto is null)
            {
                MessageThread = null;
                return;
            }

            MessageThread = new CaseMessageThreadViewModel(
                dto.ReturnRequestId,
                dto.CaseNumber,
                dto.Messages.Select(m => new CaseMessageViewModel(
                    m.MessageId,
                    m.SenderId,
                    m.SenderRole,
                    m.SenderName,
                    m.Content,
                    m.SentAt,
                    m.IsRead,
                    m.SenderId == userId)).ToList().AsReadOnly(),
                dto.UnreadCount);

            // Mark messages as read
            if (dto.UnreadCount > 0)
            {
                var markReadCommand = new MarkCaseMessagesReadCommand(caseId, userId, "admin");
                await _caseMessageService.HandleAsync(markReadCommand);
            }
        }

        private void LoadOptions()
        {
            // Decision type options
            DecisionTypeOptions = new List<SelectListItem>
            {
                new SelectListItem("Override Seller Decision", "OverrideSeller"),
                new SelectListItem("Enforce Refund", "EnforceRefund"),
                new SelectListItem("Close Without Action", "CloseWithoutAction")
            };

            // Resolution type options (for enforce refund)
            ResolutionTypeOptions = new List<SelectListItem>
            {
                new SelectListItem("Full Refund", "FullRefund"),
                new SelectListItem("Partial Refund", "PartialRefund"),
                new SelectListItem("No Refund", "NoRefund")
            };

            // Escalation reason options
            EscalationReasonOptions = new List<SelectListItem>
            {
                new SelectListItem("Buyer Requested", "BuyerRequested"),
                new SelectListItem("SLA Breach", "SLABreach"),
                new SelectListItem("Admin Flagged", "AdminFlagged")
            };
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        public class AdminDecisionInput
        {
            [Required(ErrorMessage = "Decision type is required")]
            public string? DecisionType { get; set; }

            [Required(ErrorMessage = "Decision notes are required")]
            [StringLength(2000, ErrorMessage = "Decision notes cannot exceed 2000 characters")]
            public string? DecisionNotes { get; set; }

            public string? ResolutionType { get; set; }

            [Range(0.01, 1000000, ErrorMessage = "Partial refund amount must be greater than zero")]
            public decimal? PartialRefundAmount { get; set; }

            public bool InitiateRefund { get; set; }
        }

        public class EscalationInput
        {
            [Required(ErrorMessage = "Escalation reason is required")]
            public string? Reason { get; set; }

            [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
            public string? Notes { get; set; }
        }
    }
}
