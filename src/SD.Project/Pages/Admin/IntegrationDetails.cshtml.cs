using System.ComponentModel.DataAnnotations;
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
public class IntegrationDetailsModel : PageModel
{
    private readonly ILogger<IntegrationDetailsModel> _logger;
    private readonly IntegrationService _integrationService;

    public IntegrationDetailsModel(ILogger<IntegrationDetailsModel> logger, IntegrationService integrationService)
    {
        _logger = logger;
        _integrationService = integrationService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public bool IsEditMode => Id.HasValue;

    public IntegrationViewModel? CurrentIntegration { get; private set; }

    [BindProperty]
    public IntegrationFormModel Form { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public string? TestConnectionResult { get; set; }
    public bool? TestConnectionSuccess { get; set; }
    public int? TestConnectionResponseTime { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (IsEditMode)
        {
            var dto = await _integrationService.HandleAsync(new GetIntegrationByIdQuery(Id!.Value));
            if (dto is null)
            {
                return NotFound();
            }

            CurrentIntegration = new IntegrationViewModel
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
            };

            // Pre-populate the form
            Form = new IntegrationFormModel
            {
                Name = dto.Name,
                Type = dto.Type,
                Environment = dto.Environment,
                Endpoint = dto.Endpoint,
                MerchantId = dto.MerchantId,
                CallbackUrl = dto.CallbackUrl,
                Description = dto.Description
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            if (IsEditMode)
            {
                await LoadCurrentIntegration();
            }
            return Page();
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            ErrorMessage = "Unable to determine current user.";
            return RedirectToPage("Integrations");
        }

        if (IsEditMode)
        {
            var command = new UpdateIntegrationCommand(
                Id!.Value,
                userId,
                Form.Name,
                Form.Type,
                Form.Environment,
                Form.Endpoint,
                Form.ApiKey,
                Form.MerchantId,
                Form.CallbackUrl,
                Form.Description);

            var result = await _integrationService.HandleAsync(command);

            if (result.IsSuccess)
            {
                SuccessMessage = "Integration updated successfully.";
                return RedirectToPage("Integrations");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                await LoadCurrentIntegration();
                return Page();
            }
        }
        else
        {
            var command = new CreateIntegrationCommand(
                userId,
                Form.Name,
                Form.Type,
                Form.Environment,
                Form.Endpoint,
                Form.ApiKey,
                Form.MerchantId,
                Form.CallbackUrl,
                Form.Description);

            var result = await _integrationService.HandleAsync(command);

            if (result.IsSuccess)
            {
                SuccessMessage = "Integration created successfully.";
                return RedirectToPage("IntegrationDetails", new { id = result.Integration!.Id });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return Page();
            }
        }
    }

    public async Task<IActionResult> OnPostTestConnectionAsync()
    {
        if (!Id.HasValue)
        {
            return BadRequest();
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            ErrorMessage = "Unable to determine current user.";
            return RedirectToPage();
        }

        var command = new TestIntegrationConnectionCommand(Id.Value, userId);
        var result = await _integrationService.HandleAsync(command);

        TestConnectionResult = result.Message;
        TestConnectionSuccess = result.IsSuccess;
        TestConnectionResponseTime = result.ResponseTimeMs;

        await LoadCurrentIntegration();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(bool enable)
    {
        if (!Id.HasValue)
        {
            return BadRequest();
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            ErrorMessage = "Unable to determine current user.";
            return RedirectToPage();
        }

        var command = new ToggleIntegrationStatusCommand(Id.Value, userId, enable);
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

        return RedirectToPage(new { id = Id });
    }

    private async Task LoadCurrentIntegration()
    {
        if (!Id.HasValue)
        {
            return;
        }

        var dto = await _integrationService.HandleAsync(new GetIntegrationByIdQuery(Id.Value));
        if (dto is not null)
        {
            CurrentIntegration = new IntegrationViewModel
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
            };
        }
    }
}

public class IntegrationFormModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is required.")]
    public IntegrationType Type { get; set; }

    [Required(ErrorMessage = "Environment is required.")]
    public IntegrationEnvironment Environment { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// API key for the integration. Only set when creating or updating the key.
    /// </summary>
    public string? ApiKey { get; set; }

    public string? MerchantId { get; set; }

    [Url(ErrorMessage = "Please enter a valid callback URL.")]
    public string? CallbackUrl { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}
