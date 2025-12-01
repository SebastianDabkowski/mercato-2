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
public class PhotoModerationModel : PageModel
{
    private readonly ILogger<PhotoModerationModel> _logger;
    private readonly PhotoModerationService _photoModerationService;

    public IReadOnlyCollection<PhotoModerationViewModel> Photos { get; private set; } = Array.Empty<PhotoModerationViewModel>();
    public PhotoModerationStatsViewModel? Stats { get; private set; }
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
    public bool? IsFlagged { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public PhotoModerationModel(
        ILogger<PhotoModerationModel> logger,
        PhotoModerationService photoModerationService)
    {
        _logger = logger;
        _photoModerationService = photoModerationService;
    }

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        LoadFilters();
        await LoadStatsAsync();
        await LoadPhotosAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed photo moderation queue, found {PhotoCount} photos",
            GetUserId(),
            TotalCount);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid photoId)
    {
        if (photoId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid photo ID." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new ApprovePhotoCommand(photoId, userId);
        var result = await _photoModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} approved photo {PhotoId}",
                GetUserId(),
                photoId);

            return RedirectToPage(new { success = "Photo approved successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid photoId, string reason)
    {
        if (photoId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Invalid photo ID." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Removal reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var command = new RemovePhotoCommand(photoId, userId, reason);
        var result = await _photoModerationService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} removed photo {PhotoId} with reason {Reason}",
                GetUserId(),
                photoId,
                reason);

            return RedirectToPage(new { success = "Photo removed successfully." });
        }

        return RedirectToPage(new { error = result.ErrorMessage });
    }

    public async Task<IActionResult> OnPostBatchApproveAsync(string photoIds)
    {
        if (string.IsNullOrWhiteSpace(photoIds))
        {
            return RedirectToPage(new { error = "No photos selected." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = photoIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid photo IDs provided." });
        }

        var command = new BatchApprovePhotosCommand(ids, userId);
        var result = await _photoModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch approved {SuccessCount}/{TotalCount} photos",
            GetUserId(),
            result.SuccessCount,
            ids.Count);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully approved {result.SuccessCount} photo(s)." });
        }

        return RedirectToPage(new { error = $"Approved {result.SuccessCount} photo(s), {result.FailureCount} failed." });
    }

    public async Task<IActionResult> OnPostBatchRemoveAsync(string photoIds, string reason)
    {
        if (string.IsNullOrWhiteSpace(photoIds))
        {
            return RedirectToPage(new { error = "No photos selected." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return RedirectToPage(new { error = "Removal reason is required." });
        }

        var userId = Guid.TryParse(GetUserId(), out var adminId) ? adminId : Guid.Empty;
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "Unable to determine admin user." });
        }

        var ids = photoIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
        {
            return RedirectToPage(new { error = "No valid photo IDs provided." });
        }

        var command = new BatchRemovePhotosCommand(ids, userId, reason);
        var result = await _photoModerationService.HandleAsync(command);

        _logger.LogInformation(
            "Admin {UserId} batch removed {SuccessCount}/{TotalCount} photos with reason {Reason}",
            GetUserId(),
            result.SuccessCount,
            ids.Count,
            reason);

        if (result.Success)
        {
            return RedirectToPage(new { success = $"Successfully removed {result.SuccessCount} photo(s)." });
        }

        return RedirectToPage(new { error = $"Removed {result.SuccessCount} photo(s), {result.FailureCount} failed." });
    }

    private async Task LoadStatsAsync()
    {
        var statsDto = await _photoModerationService.HandleAsync(
            new GetPhotoModerationStatsQuery());

        Stats = new PhotoModerationStatsViewModel(
            statsDto.PendingCount,
            statsDto.FlaggedCount,
            statsDto.ApprovedCount,
            statsDto.RemovedCount,
            statsDto.ApprovedTodayCount,
            statsDto.RemovedTodayCount);
    }

    private async Task LoadPhotosAsync()
    {
        PhotoModerationStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<PhotoModerationStatus>(Status, out var parsed))
        {
            statusFilter = parsed;
        }

        var query = new GetPhotosForModerationQuery(
            statusFilter,
            IsFlagged,
            SearchTerm,
            Page,
            PageSize);

        var result = await _photoModerationService.HandleAsync(query);

        Photos = result.Items.Select(p => new PhotoModerationViewModel(
            p.PhotoId,
            p.ProductId,
            p.StoreId,
            p.FileName,
            p.ImageUrl,
            p.ThumbnailUrl,
            p.ModerationStatus.ToString(),
            p.ModerationRemovalReason,
            p.IsFlagged,
            p.FlagReason,
            p.FlaggedAt,
            p.LastModeratorId,
            p.LastModeratorName,
            p.LastModeratedAt,
            p.ProductName,
            p.StoreName,
            p.SellerName,
            p.SellerEmail,
            p.IsMain,
            p.CreatedAt)).ToList();

        CurrentPage = result.PageNumber;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
    }

    private void LoadFilters()
    {
        StatusOptions = new List<SelectListItem>
        {
            new SelectListItem("All Statuses", ""),
            new SelectListItem("Pending Review", "PendingReview"),
            new SelectListItem("Approved", "Approved"),
            new SelectListItem("Removed", "Removed")
        };
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }
}
