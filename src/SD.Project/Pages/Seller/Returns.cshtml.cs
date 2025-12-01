using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for displaying seller's return and complaint cases.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class ReturnsModel : PageModel
{
    private readonly ILogger<ReturnsModel> _logger;
    private readonly ReturnRequestService _returnRequestService;
    private readonly StoreService _storeService;

    public string? StoreName { get; private set; }
    public PagedResultDto<SellerReturnRequestSummaryDto>? ReturnRequests { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public ReturnsModel(
        ILogger<ReturnsModel> logger,
        ReturnRequestService returnRequestService,
        StoreService storeService)
    {
        _logger = logger;
        _returnRequestService = returnRequestService;
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

        StoreName = store.Name;

        // Get return requests
        ReturnRequests = await _returnRequestService.HandleAsync(
            new GetSellerReturnRequestsQuery(
                store.Id,
                Status,
                FromDate,
                ToDate,
                PageNumber,
                20,
                Type),
            cancellationToken);

        _logger.LogInformation("Seller returns page viewed for store {StoreId}, found {Count} cases",
            store.Id, ReturnRequests.TotalCount);

        return Page();
    }
}
