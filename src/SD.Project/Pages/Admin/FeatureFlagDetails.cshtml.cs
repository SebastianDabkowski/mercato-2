using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class FeatureFlagDetailsModel : PageModel
    {
        private const int AuditLogPageSize = 10;
        private readonly ILogger<FeatureFlagDetailsModel> _logger;
        private readonly FeatureFlagService _featureFlagService;

        public FeatureFlagViewModel? FeatureFlag { get; private set; }
        public IReadOnlyCollection<FeatureFlagAuditLogViewModel> AuditLogs { get; private set; } = Array.Empty<FeatureFlagAuditLogViewModel>();
        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        [BindProperty]
        public EditMetadataInput EditMetadata { get; set; } = new();

        [BindProperty]
        public EditTargetingInput EditTargeting { get; set; } = new();

        [BindProperty]
        public EditEnvironmentInput EditEnvironment { get; set; } = new();

        public FeatureFlagDetailsModel(
            ILogger<FeatureFlagDetailsModel> logger,
            FeatureFlagService featureFlagService)
        {
            _logger = logger;
            _featureFlagService = featureFlagService;
        }

        public async Task<IActionResult> OnGetAsync(
            Guid id,
            string? success = null,
            string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            await LoadFeatureFlagAsync(id);

            if (FeatureFlag is not null)
            {
                _logger.LogInformation(
                    "Admin {UserId} viewed feature flag {FeatureFlagKey}",
                    GetUserId(),
                    FeatureFlag.Key);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostEnableAsync(Guid id)
        {
            var command = new EnableFeatureFlagCommand(
                id,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} enabled feature flag {FeatureFlagId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = result.Message });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostDisableAsync(Guid id)
        {
            var command = new DisableFeatureFlagCommand(
                id,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} disabled feature flag {FeatureFlagId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = result.Message });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostEnableTargetingAsync(Guid id)
        {
            var command = new EnableFeatureFlagTargetingCommand(
                id,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} enabled targeting for feature flag {FeatureFlagId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = result.Message });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostSetGlobalOverrideAsync(Guid id, bool enabled)
        {
            var command = new SetFeatureFlagGlobalOverrideCommand(
                id,
                enabled,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} set global override to {Enabled} for feature flag {FeatureFlagId}",
                    GetUserId(),
                    enabled,
                    id);

                return RedirectToPage(new { id, success = result.Message });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostUpdateMetadataAsync(Guid id)
        {
            if (!ModelState.IsValid)
            {
                await LoadFeatureFlagAsync(id);
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new UpdateFeatureFlagCommand(
                id,
                EditMetadata.Name!,
                EditMetadata.Description ?? string.Empty,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated metadata for feature flag {FeatureFlagId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = "Feature flag updated successfully." });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostUpdateTargetingAsync(Guid id)
        {
            var command = new UpdateFeatureFlagTargetingCommand(
                id,
                EditTargeting.RolloutPercentage,
                EditTargeting.TargetUserGroups,
                EditTargeting.TargetUserIds,
                EditTargeting.TargetSellerIds,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated targeting for feature flag {FeatureFlagId}",
                    GetUserId(),
                    id);

                return RedirectToPage(new { id, success = "Targeting rules updated successfully." });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostUpdateEnvironmentAsync(Guid id)
        {
            var command = new UpdateFeatureFlagEnvironmentCommand(
                id,
                EditEnvironment.Environment!,
                EditEnvironment.IsEnabled,
                EditEnvironment.RolloutPercentageOverride,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated environment {Environment} for feature flag {FeatureFlagId}",
                    GetUserId(),
                    EditEnvironment.Environment,
                    id);

                return RedirectToPage(new { id, success = $"Environment '{EditEnvironment.Environment}' settings updated." });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var command = new DeleteFeatureFlagCommand(
                id,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} deleted feature flag {FeatureFlagId}",
                    GetUserId(),
                    id);

                return RedirectToPage("/Admin/FeatureFlags", new { success = "Feature flag deleted successfully." });
            }

            return RedirectToPage(new { id, error = string.Join(" ", result.Errors) });
        }

        private async Task LoadFeatureFlagAsync(Guid id)
        {
            var query = new GetFeatureFlagByIdQuery(id);
            var dto = await _featureFlagService.HandleAsync(query);

            if (dto is not null)
            {
                FeatureFlag = MapToViewModel(dto);

                // Load audit logs
                var auditQuery = new GetFeatureFlagAuditLogsQuery(id, PageNumber: 1, PageSize: AuditLogPageSize);
                var auditResult = await _featureFlagService.HandleAsync(auditQuery);
                AuditLogs = auditResult.Items.Select(MapToAuditLogViewModel).ToList().AsReadOnly();
            }
        }

        private static FeatureFlagViewModel MapToViewModel(Application.DTOs.FeatureFlagDto dto)
        {
            return new FeatureFlagViewModel(
                dto.Id,
                dto.Key,
                dto.Name,
                dto.Description,
                dto.Status,
                dto.GlobalOverride,
                dto.RolloutPercentage,
                dto.TargetUserGroups,
                dto.TargetUserIds,
                dto.TargetSellerIds,
                dto.CreatedByUserId,
                dto.LastModifiedByUserId,
                dto.CreatedAt,
                dto.UpdatedAt,
                dto.Environments.Select(e => new FeatureFlagEnvironmentViewModel(
                    e.Id,
                    e.FeatureFlagId,
                    e.Environment,
                    e.IsEnabled,
                    e.RolloutPercentageOverride,
                    e.LastModifiedByUserId,
                    e.CreatedAt,
                    e.UpdatedAt)).ToList().AsReadOnly());
        }

        private static FeatureFlagAuditLogViewModel MapToAuditLogViewModel(Application.DTOs.FeatureFlagAuditLogDto dto)
        {
            return new FeatureFlagAuditLogViewModel(
                dto.Id,
                dto.FeatureFlagId,
                dto.FeatureFlagKey,
                dto.Action,
                dto.PerformedByUserId,
                dto.PerformedByUserRole,
                dto.PreviousValue,
                dto.NewValue,
                dto.Environment,
                dto.Details,
                dto.IpAddress,
                dto.OccurredAt);
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        private Guid GetUserGuid()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
        }

        private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

        public class EditMetadataInput
        {
            [Required(ErrorMessage = "Name is required")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
            public string? Name { get; set; }

            [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
            public string? Description { get; set; }
        }

        public class EditTargetingInput
        {
            [Range(0, 100, ErrorMessage = "Rollout percentage must be between 0 and 100")]
            public int RolloutPercentage { get; set; }

            [StringLength(1000, ErrorMessage = "Target user groups cannot exceed 1000 characters")]
            public string? TargetUserGroups { get; set; }

            [StringLength(4000, ErrorMessage = "Target user IDs cannot exceed 4000 characters")]
            public string? TargetUserIds { get; set; }

            [StringLength(4000, ErrorMessage = "Target seller IDs cannot exceed 4000 characters")]
            public string? TargetSellerIds { get; set; }
        }

        public class EditEnvironmentInput
        {
            [Required]
            public string? Environment { get; set; }

            public bool IsEnabled { get; set; }

            [Range(0, 100, ErrorMessage = "Rollout percentage must be between 0 and 100")]
            public int? RolloutPercentageOverride { get; set; }
        }
    }
}
