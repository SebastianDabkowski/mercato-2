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

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin)]
public class SellerRatingModerationModel : PageModel
{
    private readonly ILogger<SellerRatingModerationModel> _logger;
    private readonly SellerRatingModerationService _sellerRatingModerationService;

    public IReadOnlyCollection<AdminSellerRatingModerationViewModel> SellerRatings { get; private set; } = Array.Empty<AdminSellerRatingModerationViewModel>();
    public SellerRatingModerationStatsViewModel? Stats { get; private set; }
    public IReadOnlyCollection<SelectListItem> StatusOptions { get; private set; } = Array.Empty<SelectListItem>();

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
    public string? FlaggedOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public SellerRatingModerationModel(
        ILogger<SellerRatingModerationModel> logger,
        SellerRatingModerationService sellerRatingModerationService)
    {
        _logger = logger;
        _sellerRatingModerationService = sellerRatingModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        LoadFilters();
        await LoadStatsAsync();
        await LoadSellerRatingsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed seller rating moderation, found {RatingCount} ratings",
            GetUserId(),
            TotalCount);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid sellerRatingId)
    {
        if (sellerRatingId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid seller rating ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new ApproveSellerRatingCommand(sellerRatingId, userId);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved seller rating {SellerRatingId}",
                GetUserId(),
                sellerRatingId);

            return RedirectToPage(new { success = "Seller rating approved successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid sellerRatingId, string reason)
    {
        if (sellerRatingId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid seller rating ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Rejection reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new RejectSellerRatingCommand(sellerRatingId, userId, reason);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} rejected seller rating {SellerRatingId} with reason {Reason}",
                GetUserId(),
                sellerRatingId,
                reason);

            return RedirectToPage(new { success = "Seller rating rejected successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostFlagAsync(Guid sellerRatingId, string reason)
    {
        if (sellerRatingId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid seller rating ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Flag reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new FlagSellerRatingCommand(sellerRatingId, userId, reason);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} flagged seller rating {SellerRatingId} with reason {Reason}",
                GetUserId(),
                sellerRatingId,
                reason);

            return RedirectToPage(new { success = "Seller rating flagged successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostClearFlagAsync(Guid sellerRatingId)
    {
        if (sellerRatingId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid seller rating ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new ClearSellerRatingFlagCommand(sellerRatingId, userId);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} cleared flag on seller rating {SellerRatingId}",
                GetUserId(),
                sellerRatingId);

            return RedirectToPage(new { success = "Flag cleared successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostBatchApproveAsync(string sellerRatingIds)
    {
        if (string.IsNullOrWhiteSpace(sellerRatingIds))
        {
            return RedirectToPage(new { error = "No seller ratings selected." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = sellerRatingIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid seller rating IDs provided." });
        }

        var command = new BatchApproveSellerRatingsCommand(ids, userId);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch approved {SuccessCount}/{TotalCount} seller ratings",
            GetUserId(),
            result.SuccessCount,
            ids.Count);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully approved {result.SuccessCount} seller ratings." });
        }

        return RedirectToPage(new { error = $"Approved {result.SuccessCount} seller ratings, {result.FailureCount} failed." });
    }

    public async Task<IActionResult> OnPostBatchRejectAsync(string sellerRatingIds, string reason)
    {
        if (string.IsNullOrWhiteSpace(sellerRatingIds))
        {
            return RedirectToPage(new { error = "No seller ratings selected." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Rejection reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = sellerRatingIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid seller rating IDs provided." });
        }

        var command = new BatchRejectSellerRatingsCommand(ids, userId, reason);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch rejected {SuccessCount}/{TotalCount} seller ratings with reason {Reason}",
            GetUserId(),
            result.SuccessCount,
            ids.Count,
            reason);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully rejected {result.SuccessCount} seller ratings." });
        }

        return RedirectToPage(new { error = $"Rejected {result.SuccessCount} seller ratings, {result.FailureCount} failed." });
    }

    private async Task LoadStatsAsync()
    {
        var statsDto = await _sellerRatingModerationService.HandleAsync(
            new GetSellerRatingModerationStatsQuery());

        Stats = new SellerRatingModerationStatsViewModel(
            statsDto.PendingCount,
            statsDto.FlaggedCount,
            statsDto.ReportedCount,
            statsDto.ApprovedTodayCount,
            statsDto.RejectedTodayCount);
    }

    private async Task LoadSellerRatingsAsync()
    {
        SellerRatingModerationStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<SellerRatingModerationStatus>(Status, out var parsed))
        {
            statusFilter = parsed;
        }

        bool? flaggedFilter = FlaggedOnly == "true" ? true : null;

        var query = new GetSellerRatingsForModerationQuery(
            statusFilter,
            flaggedFilter,
            SearchTerm,
            null,
            FromDate,
            ToDate,
            Page,
            PageSize);

        var result = await _sellerRatingModerationService.HandleAsync(query);

        SellerRatings = result.Items.Select(r => new AdminSellerRatingModerationViewModel(
            r.SellerRatingId,
            r.OrderId,
            r.StoreId,
            r.BuyerId,
            r.StoreName,
            r.BuyerName,
            r.Rating,
            r.Comment,
            r.ModerationStatus,
            r.IsFlagged,
            r.FlagReason,
            r.FlaggedAt,
            r.ReportCount,
            r.RejectionReason,
            r.CreatedAt,
            r.UpdatedAt,
            r.ModeratedAt,
            r.ModeratorName)).ToList();

        CurrentPage = result.PageNumber;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
    }

    private void LoadFilters()
    {
        StatusOptions = new List<SelectListItem>
        {
            new SelectListItem("All Statuses", ""),
            new SelectListItem("Pending", "Pending"),
            new SelectListItem("Approved", "Approved"),
            new SelectListItem("Rejected", "Rejected")
        };
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
