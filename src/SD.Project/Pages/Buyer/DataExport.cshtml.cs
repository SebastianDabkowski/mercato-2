using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page for requesting and downloading GDPR data exports.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Seller, UserRole.Admin)]
public class DataExportModel : PageModel
{
    private readonly ILogger<DataExportModel> _logger;
    private readonly UserDataExportService _userDataExportService;

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyCollection<UserDataExportDto> PreviousExports { get; private set; } = Array.Empty<UserDataExportDto>();
    public bool HasPendingExport { get; private set; }
    public bool IsSeller { get; private set; }

    public DataExportModel(
        ILogger<DataExportModel> logger,
        UserDataExportService userDataExportService)
    {
        _logger = logger;
        _userDataExportService = userDataExportService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        await LoadDataAsync(userId);

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            SuccessMessage = StatusMessage;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRequestExportAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new RequestUserDataExportCommand(
            userId,
            ipAddress,
            userAgent);

        var result = await _userDataExportService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "User {UserId} requested a data export, export ID: {ExportId}",
                userId, result.Export?.Id);
            StatusMessage = result.Message ?? "Your data export has been generated and is ready for download.";
        }
        else
        {
            StatusMessage = string.Join(", ", result.Errors);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadAsync(Guid exportId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var downloadQuery = new DownloadUserDataExportQuery(exportId, userId);
        var downloadResult = await _userDataExportService.HandleAsync(downloadQuery);

        if (downloadResult is null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to download export {ExportId} but it was not found or not authorized",
                userId, exportId);
            StatusMessage = "Export not found or no longer available for download.";
            return RedirectToPage();
        }

        _logger.LogInformation(
            "User {UserId} downloaded data export {ExportId}",
            userId, exportId);

        var bytes = Encoding.UTF8.GetBytes(downloadResult.ExportData);
        return File(bytes, "application/json", downloadResult.FileName);
    }

    private async Task LoadDataAsync(Guid userId)
    {
        PreviousExports = await _userDataExportService.HandleAsync(
            new GetUserDataExportsQuery(userId));

        HasPendingExport = PreviousExports.Any(e =>
            e.Status == UserDataExportStatus.Pending ||
            e.Status == UserDataExportStatus.Processing);

        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        IsSeller = roleClaim == UserRole.Seller.ToString();
    }
}
