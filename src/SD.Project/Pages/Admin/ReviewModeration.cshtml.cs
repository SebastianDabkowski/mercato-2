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
public class ReviewModerationModel : PageModel
{
    private readonly ILogger<ReviewModerationModel> _logger;
    private readonly ReviewModerationService _reviewModerationService;

    public IReadOnlyCollection<AdminReviewModerationViewModel> Reviews { get; private set; } = Array.Empty<AdminReviewModerationViewModel>();
    public ReviewModerationStatsViewModel? Stats { get; private set; }
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

    public ReviewModerationModel(
        ILogger<ReviewModerationModel> logger,
        ReviewModerationService reviewModerationService)
    {
        _logger = logger;
        _reviewModerationService = reviewModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        LoadFilters();
        await LoadStatsAsync();
        await LoadReviewsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed review moderation, found {ReviewCount} reviews",
            GetUserId(),
            TotalCount);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid reviewId)
    {
        if (reviewId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid review ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new ApproveReviewCommand(reviewId, userId);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved review {ReviewId}",
                GetUserId(),
                reviewId);

            return RedirectToPage(new { success = "Review approved successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid reviewId, string reason)
    {
        if (reviewId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid review ID." });
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

        var command = new RejectReviewCommand(reviewId, userId, reason);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} rejected review {ReviewId} with reason {Reason}",
                GetUserId(),
                reviewId,
                reason);

            return RedirectToPage(new { success = "Review rejected successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostFlagAsync(Guid reviewId, string reason)
    {
        if (reviewId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid review ID." });
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

        var command = new FlagReviewCommand(reviewId, userId, reason);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} flagged review {ReviewId} with reason {Reason}",
                GetUserId(),
                reviewId,
                reason);

            return RedirectToPage(new { success = "Review flagged successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostClearFlagAsync(Guid reviewId)
    {
        if (reviewId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid review ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new ClearReviewFlagCommand(reviewId, userId);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} cleared flag on review {ReviewId}",
                GetUserId(),
                reviewId);

            return RedirectToPage(new { success = "Flag cleared successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostBatchApproveAsync(string reviewIds)
    {
        if (string.IsNullOrWhiteSpace(reviewIds))
        {
            return RedirectToPage(new { error = "No reviews selected." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = reviewIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid review IDs provided." });
        }

        var command = new BatchApproveReviewsCommand(ids, userId);
        var result = await _reviewModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch approved {SuccessCount}/{TotalCount} reviews",
            GetUserId(),
            result.SuccessCount,
            ids.Count);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully approved {result.SuccessCount} reviews." });
        }

        return RedirectToPage(new { error = $"Approved {result.SuccessCount} reviews, {result.FailureCount} failed." });
    }

    public async Task<IActionResult> OnPostBatchRejectAsync(string reviewIds, string reason)
    {
        if (string.IsNullOrWhiteSpace(reviewIds))
        {
            return RedirectToPage(new { error = "No reviews selected." });
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

        var ids = reviewIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid review IDs provided." });
        }

        var command = new BatchRejectReviewsCommand(ids, userId, reason);
        var result = await _reviewModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch rejected {SuccessCount}/{TotalCount} reviews with reason {Reason}",
            GetUserId(),
            result.SuccessCount,
            ids.Count,
            reason);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully rejected {result.SuccessCount} reviews." });
        }

        return RedirectToPage(new { error = $"Rejected {result.SuccessCount} reviews, {result.FailureCount} failed." });
    }

    private async Task LoadStatsAsync()
    {
        var statsDto = await _reviewModerationService.HandleAsync(
            new GetReviewModerationStatsQuery());

        Stats = new ReviewModerationStatsViewModel(
            statsDto.PendingCount,
            statsDto.FlaggedCount,
            statsDto.ReportedCount,
            statsDto.ApprovedTodayCount,
            statsDto.RejectedTodayCount);
    }

    private async Task LoadReviewsAsync()
    {
        ReviewModerationStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<ReviewModerationStatus>(Status, out var parsed))
        {
            statusFilter = parsed;
        }

        bool? flaggedFilter = FlaggedOnly == "true" ? true : null;

        var query = new GetReviewsForModerationQuery(
            statusFilter,
            flaggedFilter,
            SearchTerm,
            null,
            FromDate,
            ToDate,
            Page,
            PageSize);

        var result = await _reviewModerationService.HandleAsync(query);

        Reviews = result.Items.Select(r => new AdminReviewModerationViewModel(
            r.ReviewId,
            r.ProductId,
            r.StoreId,
            r.BuyerId,
            r.ProductName,
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
