using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying buyer's case (return/complaint) details.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Admin)]
public class CaseDetailsModel : PageModel
{
    private readonly ILogger<CaseDetailsModel> _logger;
    private readonly ReturnRequestService _returnRequestService;
    private readonly CaseMessageService _caseMessageService;

    public BuyerCaseDetailsViewModel? CaseDetails { get; private set; }
    public CaseMessageThreadViewModel? MessageThread { get; private set; }

    [BindProperty]
    [Required(ErrorMessage = "Message content is required")]
    [MaxLength(5000, ErrorMessage = "Message cannot exceed 5000 characters")]
    public string? MessageContent { get; set; }

    public CaseDetailsModel(
        ILogger<CaseDetailsModel> logger,
        ReturnRequestService returnRequestService,
        CaseMessageService caseMessageService)
    {
        _logger = logger;
        _returnRequestService = returnRequestService;
        _caseMessageService = caseMessageService;
    }

    public async Task<IActionResult> OnGetAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var buyerId))
        {
            return RedirectToPage("/Login");
        }

        await LoadCaseDetailsAsync(caseId, buyerId, cancellationToken);

        if (CaseDetails is not null)
        {
            // Mark messages as read when viewing
            await _caseMessageService.HandleAsync(
                new MarkCaseMessagesReadCommand(caseId, buyerId, "Buyer"),
                cancellationToken);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendMessageAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var buyerId))
        {
            return RedirectToPage("/Login");
        }

        if (string.IsNullOrWhiteSpace(MessageContent))
        {
            TempData["MessageError"] = "Message content is required.";
            return RedirectToPage(new { caseId });
        }

        var result = await _caseMessageService.HandleAsync(
            new SendCaseMessageCommand(caseId, buyerId, "Buyer", MessageContent),
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData["MessageSuccess"] = "Message sent successfully.";
            _logger.LogInformation("Buyer {BuyerId} sent message in case {CaseId}", buyerId, caseId);
        }
        else
        {
            TempData["MessageError"] = result.ErrorMessage ?? "Failed to send message.";
            _logger.LogWarning("Failed to send message in case {CaseId}: {Error}", caseId, result.ErrorMessage);
        }

        return RedirectToPage(new { caseId });
    }

    private async Task LoadCaseDetailsAsync(Guid caseId, Guid buyerId, CancellationToken cancellationToken)
    {
        // Get case details
        var caseDetails = await _returnRequestService.HandleAsync(
            new GetBuyerReturnRequestQuery(buyerId, caseId),
            cancellationToken);

        if (caseDetails is null)
        {
            _logger.LogWarning("Case {CaseId} not found for buyer {BuyerId}", caseId, buyerId);
            return;
        }

        // Map items
        var items = caseDetails.Items
            .Select(i => new ReturnRequestItemViewModel(
                i.ItemId,
                i.OrderItemId,
                i.ProductName,
                i.Quantity))
            .ToList()
            .AsReadOnly();

        // Map linked refunds
        var linkedRefunds = caseDetails.LinkedRefunds?
            .Select(r => new LinkedRefundViewModel(
                r.RefundId,
                r.Status,
                r.Amount,
                r.Currency,
                r.RefundTransactionId,
                r.CreatedAt,
                r.CompletedAt))
            .ToList()
            .AsReadOnly();

        CaseDetails = new BuyerCaseDetailsViewModel(
            caseDetails.ReturnRequestId,
            caseDetails.OrderId,
            caseDetails.ShipmentId,
            caseDetails.CaseNumber,
            caseDetails.OrderNumber,
            caseDetails.StoreName,
            caseDetails.Type,
            caseDetails.Status,
            caseDetails.Reason,
            caseDetails.Comments,
            caseDetails.SellerResponse,
            caseDetails.CreatedAt,
            caseDetails.ApprovedAt,
            caseDetails.RejectedAt,
            caseDetails.CompletedAt,
            items,
            linkedRefunds,
            caseDetails.ResolutionType,
            caseDetails.ResolutionNotes,
            caseDetails.PartialRefundAmount,
            caseDetails.ResolvedAt);

        // Get messages
        var messageThread = await _caseMessageService.HandleAsync(
            new GetCaseMessagesQuery(caseId, buyerId, "Buyer"),
            cancellationToken);

        if (messageThread is not null)
        {
            var messages = messageThread.Messages
                .Select(m => new CaseMessageViewModel(
                    m.MessageId,
                    m.SenderId,
                    m.SenderRole,
                    m.SenderName,
                    m.Content,
                    m.SentAt,
                    m.IsRead,
                    m.SenderId == buyerId))
                .ToList()
                .AsReadOnly();

            MessageThread = new CaseMessageThreadViewModel(
                messageThread.ReturnRequestId,
                messageThread.CaseNumber,
                messages,
                messageThread.UnreadCount);
        }

        _logger.LogInformation("Buyer {BuyerId} viewed case {CaseId}", buyerId, caseId);
    }
}
