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
/// Page model for displaying buyer's return and complaint cases.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Admin)]
public class CasesModel : PageModel
{
    private readonly ILogger<CasesModel> _logger;
    private readonly ReturnRequestService _returnRequestService;

    public IReadOnlyList<BuyerCaseSummaryViewModel> Cases { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public CasesModel(
        ILogger<CasesModel> logger,
        ReturnRequestService returnRequestService)
    {
        _logger = logger;
        _returnRequestService = returnRequestService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var buyerId))
        {
            return RedirectToPage("/Login");
        }

        // Get all cases for the buyer
        var cases = await _returnRequestService.HandleAsync(
            new GetBuyerReturnRequestsQuery(buyerId),
            cancellationToken);

        // Apply filters
        var filteredCases = cases.AsEnumerable();

        if (!string.IsNullOrEmpty(Status))
        {
            filteredCases = filteredCases.Where(c => c.Status.Equals(Status, StringComparison.OrdinalIgnoreCase));
        }

        if (FromDate.HasValue)
        {
            filteredCases = filteredCases.Where(c => c.CreatedAt >= FromDate.Value);
        }

        if (ToDate.HasValue)
        {
            filteredCases = filteredCases.Where(c => c.CreatedAt <= ToDate.Value.AddDays(1).AddSeconds(-1));
        }

        // Map to view models
        Cases = filteredCases
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new BuyerCaseSummaryViewModel(
                c.ReturnRequestId,
                c.OrderId,
                c.CaseNumber,
                c.OrderNumber,
                c.StoreName,
                c.Type,
                c.Status,
                c.CreatedAt))
            .ToList()
            .AsReadOnly();

        _logger.LogInformation("Buyer {BuyerId} viewed cases list with {CaseCount} cases", buyerId, Cases.Count);

        return Page();
    }

    private Guid? GetBuyerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var buyerId))
        {
            return buyerId;
        }
        return null;
    }
}
