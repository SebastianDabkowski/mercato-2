using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

[RequireRole(UserRole.Admin)]
public class ReviewModerationDetailsModel : PageModel
{
    private readonly ILogger<ReviewModerationDetailsModel> _logger;
    private readonly ReviewModerationService _reviewModerationService;

    public AdminReviewModerationViewModel? Review { get; private set; }
    public IReadOnlyCollection<ReviewModerationAuditLogViewModel> AuditLogs { get; private set; } = Array.Empty<ReviewModerationAuditLogViewModel>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ReviewModerationDetailsModel(
        ILogger<ReviewModerationDetailsModel> logger,
        ReviewModerationService reviewModerationService)
    {
        _logger = logger;
        _reviewModerationService = reviewModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage("/Admin/ReviewModeration", new { error = "Invalid review ID." });
        }

        SuccessMessage = success;
        ErrorMessage = error;

        await LoadReviewAsync();

        if (Review is null)
        {
            return RedirectToPage("/Admin/ReviewModeration", new { error = "Review not found." });
        }

        await LoadAuditLogsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed review moderation details for {ReviewId}",
            GetUserId(),
            Id);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid review ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new ApproveReviewCommand(Id, userId);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved review {ReviewId}",
                GetUserId(),
                Id);

            return RedirectToPage(new { id = Id, success = "Review approved successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRejectAsync(string reason)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid review ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { id = Id, error = "Rejection reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new RejectReviewCommand(Id, userId, reason);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} rejected review {ReviewId} with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Review rejected successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostFlagAsync(string reason)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid review ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { id = Id, error = "Flag reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new FlagReviewCommand(Id, userId, reason);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} flagged review {ReviewId} with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Review flagged successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostClearFlagAsync()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid review ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new ClearReviewFlagCommand(Id, userId);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} cleared flag on review {ReviewId}",
                GetUserId(),
                Id);

            return RedirectToPage(new { id = Id, success = "Flag cleared successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostResetToPendingAsync(string reason)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid review ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { id = Id, error = "Reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new ResetReviewToPendingCommand(Id, userId, reason);
        var result = await _reviewModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} reset review {ReviewId} to pending with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Review reset to pending status." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    private async Task LoadReviewAsync()
    {
        var dto = await _reviewModerationService.HandleAsync(
            new GetReviewForModerationQuery(Id));

        if (dto is null)
        {
            return;
        }

        Review = new AdminReviewModerationViewModel(
            dto.ReviewId,
            dto.ProductId,
            dto.StoreId,
            dto.BuyerId,
            dto.ProductName,
            dto.StoreName,
            dto.BuyerName,
            dto.Rating,
            dto.Comment,
            dto.ModerationStatus,
            dto.IsFlagged,
            dto.FlagReason,
            dto.FlaggedAt,
            dto.ReportCount,
            dto.RejectionReason,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.ModeratedAt,
            dto.ModeratorName);
    }

    private async Task LoadAuditLogsAsync()
    {
        var result = await _reviewModerationService.HandleAsync(
            new GetReviewModerationAuditLogsQuery(Id));

        AuditLogs = result.Items.Select(l => new ReviewModerationAuditLogViewModel(
            l.Id,
            l.ReviewId,
            l.ModeratorName,
            l.Action,
            l.PreviousStatus,
            l.NewStatus,
            l.Reason,
            l.Notes,
            l.IsAutomated,
            l.AutomatedRuleName,
            l.CreatedAt)).ToList();
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
