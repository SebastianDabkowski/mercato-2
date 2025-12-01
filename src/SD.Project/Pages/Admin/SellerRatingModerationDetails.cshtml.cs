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
public class SellerRatingModerationDetailsModel : PageModel
{
    private readonly ILogger<SellerRatingModerationDetailsModel> _logger;
    private readonly SellerRatingModerationService _sellerRatingModerationService;

    public AdminSellerRatingModerationViewModel? SellerRating { get; private set; }
    public IReadOnlyCollection<SellerRatingModerationAuditLogViewModel> AuditLogs { get; private set; } = Array.Empty<SellerRatingModerationAuditLogViewModel>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public SellerRatingModerationDetailsModel(
        ILogger<SellerRatingModerationDetailsModel> logger,
        SellerRatingModerationService sellerRatingModerationService)
    {
        _logger = logger;
        _sellerRatingModerationService = sellerRatingModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage("/Admin/SellerRatingModeration", new { error = "Invalid seller rating ID." });
        }

        SuccessMessage = success;
        ErrorMessage = error;

        await LoadSellerRatingAsync();

        if (SellerRating is null)
        {
            return RedirectToPage("/Admin/SellerRatingModeration", new { error = "Seller rating not found." });
        }

        await LoadAuditLogsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed seller rating moderation details for {SellerRatingId}",
            GetUserId(),
            Id);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid seller rating ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new ApproveSellerRatingCommand(Id, userId);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved seller rating {SellerRatingId}",
                GetUserId(),
                Id);

            return RedirectToPage(new { id = Id, success = "Seller rating approved successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRejectAsync(string reason)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid seller rating ID." });
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

        var command = new RejectSellerRatingCommand(Id, userId, reason);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} rejected seller rating {SellerRatingId} with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Seller rating rejected successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostFlagAsync(string reason)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid seller rating ID." });
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

        var command = new FlagSellerRatingCommand(Id, userId, reason);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} flagged seller rating {SellerRatingId} with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Seller rating flagged successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostClearFlagAsync()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid seller rating ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new ClearSellerRatingFlagCommand(Id, userId);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} cleared flag on seller rating {SellerRatingId}",
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
            return RedirectToPage(new { id = Id, error = "Invalid seller rating ID." });
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

        var command = new ResetSellerRatingToPendingCommand(Id, userId, reason);
        var result = await _sellerRatingModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} reset seller rating {SellerRatingId} to pending with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Seller rating reset to pending status." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    private async Task LoadSellerRatingAsync()
    {
        var dto = await _sellerRatingModerationService.HandleAsync(
            new GetSellerRatingForModerationQuery(Id));

        if (dto is null)
        {
            return;
        }

        SellerRating = new AdminSellerRatingModerationViewModel(
            dto.SellerRatingId,
            dto.OrderId,
            dto.StoreId,
            dto.BuyerId,
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
        var result = await _sellerRatingModerationService.HandleAsync(
            new GetSellerRatingModerationAuditLogsQuery(Id));

        AuditLogs = result.Items.Select(l => new SellerRatingModerationAuditLogViewModel(
            l.Id,
            l.SellerRatingId,
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
