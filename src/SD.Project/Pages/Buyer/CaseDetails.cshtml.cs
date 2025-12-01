using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
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

    public BuyerCaseDetailsViewModel? CaseDetails { get; private set; }

    public CaseDetailsModel(
        ILogger<CaseDetailsModel> logger,
        ReturnRequestService returnRequestService)
    {
        _logger = logger;
        _returnRequestService = returnRequestService;
    }

    public async Task<IActionResult> OnGetAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var buyerId))
        {
            return RedirectToPage("/Login");
        }

        // Get case details
        var caseDetails = await _returnRequestService.HandleAsync(
            new GetBuyerReturnRequestQuery(buyerId, caseId),
            cancellationToken);

        if (caseDetails is null)
        {
            _logger.LogWarning("Case {CaseId} not found for buyer {BuyerId}", caseId, buyerId);
            return Page();
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
            linkedRefunds);

        _logger.LogInformation("Buyer {BuyerId} viewed case {CaseId}", buyerId, caseId);

        return Page();
    }
}
