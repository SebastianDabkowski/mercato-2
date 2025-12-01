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
public class PhotoModerationDetailsModel : PageModel
{
    private readonly ILogger<PhotoModerationDetailsModel> _logger;
    private readonly PhotoModerationService _photoModerationService;

    public PhotoModerationViewModel? Photo { get; private set; }
    public IReadOnlyCollection<PhotoModerationAuditLogViewModel> AuditLogs { get; private set; } = Array.Empty<PhotoModerationAuditLogViewModel>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PhotoModerationDetailsModel(
        ILogger<PhotoModerationDetailsModel> logger,
        PhotoModerationService photoModerationService)
    {
        _logger = logger;
        _photoModerationService = photoModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage("/Admin/PhotoModeration", new { error = "Invalid photo ID." });
        }

        SuccessMessage = success;
        ErrorMessage = error;

        await LoadPhotoAsync();

        if (Photo is null)
        {
            return RedirectToPage("/Admin/PhotoModeration", new { error = "Photo not found." });
        }

        await LoadAuditLogsAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed photo moderation details for photo {PhotoId}",
            GetUserId(),
            Id);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid photo ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new ApprovePhotoCommand(Id, userId);
        var result = await _photoModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved photo {PhotoId}",
                GetUserId(),
                Id);

            return RedirectToPage(new { id = Id, success = "Photo approved successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string reason)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Invalid photo ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { id = Id, error = "Removal reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id, error = "Unable to determine admin user." });
        }

        var command = new RemovePhotoCommand(Id, userId, reason);
        var result = await _photoModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} removed photo {PhotoId} with reason {Reason}",
                GetUserId(),
                Id,
                reason);

            return RedirectToPage(new { id = Id, success = "Photo removed successfully." });
        }

        return RedirectToPage(new { id = Id, error = result.ErrorMessage });
    }

    private async Task LoadPhotoAsync()
    {
        var dto = await _photoModerationService.HandleAsync(
            new GetPhotoForModerationDetailsQuery(Id));

        if (dto is null)
        {
            return;
        }

        Photo = new PhotoModerationViewModel(
            dto.PhotoId,
            dto.ProductId,
            dto.StoreId,
            dto.FileName,
            dto.ImageUrl,
            dto.ThumbnailUrl,
            dto.ModerationStatus.ToString(),
            dto.ModerationRemovalReason,
            dto.IsFlagged,
            dto.FlagReason,
            dto.FlaggedAt,
            dto.LastModeratorId,
            dto.LastModeratorName,
            dto.LastModeratedAt,
            dto.ProductName,
            dto.StoreName,
            dto.SellerName,
            dto.SellerEmail,
            dto.IsMain,
            dto.CreatedAt);
    }

    private async Task LoadAuditLogsAsync()
    {
        var logs = await _photoModerationService.HandleAsync(
            new GetPhotoModerationHistoryQuery(Id));

        AuditLogs = logs.Select(l => new PhotoModerationAuditLogViewModel(
            l.Id,
            l.PhotoId,
            l.ModeratorId,
            l.ModeratorName,
            l.Decision.ToString(),
            l.Reason,
            l.CreatedAt)).ToList();
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
