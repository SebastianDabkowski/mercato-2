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
public class IntegrationsModel : PageModel
{
    private readonly ILogger<IntegrationsModel> _logger;
    private readonly IntegrationService _integrationService;

    public IntegrationsModel(ILogger<IntegrationsModel> logger, IntegrationService integrationService)
    {
        _logger = logger;
        _integrationService = integrationService;
    }

    public IReadOnlyList<IntegrationViewModel> Integrations { get; private set; } = Array.Empty<IntegrationViewModel>();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }

    [BindProperty(SupportsGet = true)]
    public IntegrationType? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public IntegrationStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public IntegrationEnvironment? EnvironmentFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin integrations list accessed by user {UserId}",
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        var query = new GetIntegrationsQuery(
            TypeFilter,
            StatusFilter,
            EnvironmentFilter,
            SearchTerm,
            PageNumber,
            PageSize);

        var result = await _integrationService.HandleAsync(query);

        Integrations = result.Items.Select(dto => new IntegrationViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Type = dto.Type,
            Status = dto.Status,
            Environment = dto.Environment,
            Endpoint = dto.Endpoint,
            MerchantId = dto.MerchantId,
            CallbackUrl = dto.CallbackUrl,
            Description = dto.Description,
            MaskedApiKey = dto.MaskedApiKey,
            HasApiKey = dto.HasApiKey,
            LastHealthCheckAt = dto.LastHealthCheckAt,
            LastHealthCheckMessage = dto.LastHealthCheckMessage,
            LastHealthCheckSuccess = dto.LastHealthCheckSuccess,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        }).ToArray();

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, bool enable)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage();
        }

        var command = new ToggleIntegrationStatusCommand(id, userId, enable);
        var result = await _integrationService.HandleAsync(command);

        if (result.IsSuccess)
        {
            SuccessMessage = enable
                ? "Integration enabled successfully."
                : "Integration disabled successfully.";
        }
        else
        {
            ErrorMessage = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestConnectionAsync(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage();
        }

        var command = new TestIntegrationConnectionCommand(id, userId);
        var result = await _integrationService.HandleAsync(command);

        if (result.IsSuccess)
        {
            SuccessMessage = $"Connection test successful: {result.Message}";
        }
        else
        {
            ErrorMessage = $"Connection test failed: {result.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage();
        }

        var command = new DeleteIntegrationCommand(id, userId);
        var result = await _integrationService.HandleAsync(command);

        if (result.IsSuccess)
        {
            SuccessMessage = "Integration deleted successfully.";
        }
        else
        {
            ErrorMessage = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out userId))
        {
            ErrorMessage = "Unable to determine current user.";
            return false;
        }
        return true;
    }
}
