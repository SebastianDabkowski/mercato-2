using System.Security.Claims;
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
/// Page for managing user consent settings (privacy settings).
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Seller, UserRole.Admin)]
public class ConsentSettingsModel : PageModel
{
    private readonly ILogger<ConsentSettingsModel> _logger;
    private readonly UserConsentService _userConsentService;

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyCollection<ConsentTypeDto> ConsentTypes { get; private set; } = Array.Empty<ConsentTypeDto>();
    public IReadOnlyCollection<UserConsentDetailDto> UserConsents { get; private set; } = Array.Empty<UserConsentDetailDto>();
    public IReadOnlyCollection<UserConsentAuditLogDto> AuditLogs { get; private set; } = Array.Empty<UserConsentAuditLogDto>();

    public ConsentSettingsModel(
        ILogger<ConsentSettingsModel> logger,
        UserConsentService userConsentService)
    {
        _logger = logger;
        _userConsentService = userConsentService;
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

    public async Task<IActionResult> OnPostGrantAsync(Guid consentTypeId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new RecordUserConsentCommand(
            userId,
            consentTypeId,
            IsGranted: true,
            Source: "settings",
            IpAddress: ipAddress,
            UserAgent: userAgent);

        var result = await _userConsentService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "User {UserId} granted consent for type {ConsentTypeId}", 
                userId, consentTypeId);
            StatusMessage = result.Message ?? "Consent granted successfully.";
        }
        else
        {
            StatusMessage = string.Join(", ", result.Errors);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostWithdrawAsync(Guid consentTypeId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new WithdrawUserConsentCommand(
            userId,
            consentTypeId,
            Source: "settings",
            IpAddress: ipAddress,
            UserAgent: userAgent);

        var result = await _userConsentService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "User {UserId} withdrew consent for type {ConsentTypeId}", 
                userId, consentTypeId);
            StatusMessage = result.Message ?? "Consent withdrawn successfully.";
        }
        else
        {
            StatusMessage = string.Join(", ", result.Errors);
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync(Guid userId)
    {
        ConsentTypes = await _userConsentService.HandleAsync(
            new GetActiveConsentTypesQuery());

        UserConsents = await _userConsentService.HandleAsync(
            new GetUserConsentsQuery(userId));

        AuditLogs = await _userConsentService.HandleAsync(
            new GetUserConsentAuditLogsQuery(userId));
    }
}
