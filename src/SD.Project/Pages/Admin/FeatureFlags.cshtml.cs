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
    public class FeatureFlagsModel : PageModel
    {
        private const int PageSize = 20;
        private readonly ILogger<FeatureFlagsModel> _logger;
        private readonly FeatureFlagService _featureFlagService;

        public IReadOnlyCollection<FeatureFlagViewModel> FeatureFlags { get; private set; } = Array.Empty<FeatureFlagViewModel>();
        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? SearchTerm { get; private set; }
        public string? StatusFilter { get; private set; }
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;

        [BindProperty]
        public CreateFlagInput NewFlag { get; set; } = new();

        public FeatureFlagsModel(
            ILogger<FeatureFlagsModel> logger,
            FeatureFlagService featureFlagService)
        {
            _logger = logger;
            _featureFlagService = featureFlagService;
        }

        public async Task<IActionResult> OnGetAsync(
            string? searchTerm = null,
            string? statusFilter = null,
            int page = 1,
            string? success = null,
            string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;
            SearchTerm = searchTerm;
            StatusFilter = statusFilter;
            CurrentPage = Math.Max(1, page);

            FeatureFlagStatus? status = statusFilter switch
            {
                "Disabled" => FeatureFlagStatus.Disabled,
                "Enabled" => FeatureFlagStatus.Enabled,
                "Targeted" => FeatureFlagStatus.Targeted,
                _ => null
            };

            var query = new GetFeatureFlagsQuery(searchTerm, status, CurrentPage, PageSize);
            var result = await _featureFlagService.HandleAsync(query);

            FeatureFlags = result.Items.Select(MapToViewModel).ToList().AsReadOnly();
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)PageSize);

            _logger.LogInformation(
                "Admin {UserId} viewed feature flags, found {FlagCount} flags",
                GetUserId(),
                result.TotalCount);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(SearchTerm, StatusFilter);
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new CreateFeatureFlagCommand(
                NewFlag.Key!,
                NewFlag.Name!,
                NewFlag.Description ?? string.Empty,
                GetUserGuid(),
                UserRole.Admin,
                GetIpAddress(),
                GetUserAgent());

            var result = await _featureFlagService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} created feature flag {FeatureFlagKey}",
                    GetUserId(),
                    result.FeatureFlag?.Key);

                return RedirectToPage(new { success = $"Feature flag '{result.FeatureFlag?.Name}' created successfully." });
            }

            await OnGetAsync(SearchTerm, StatusFilter);
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostEnableAsync(Guid flagId)
        {
            var command = new EnableFeatureFlagCommand(
                flagId,
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
                    flagId);

                return RedirectToPage(new { success = result.Message });
            }

            return RedirectToPage(new { error = string.Join(" ", result.Errors) });
        }

        public async Task<IActionResult> OnPostDisableAsync(Guid flagId)
        {
            var command = new DisableFeatureFlagCommand(
                flagId,
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
                    flagId);

                return RedirectToPage(new { success = result.Message });
            }

            return RedirectToPage(new { error = string.Join(" ", result.Errors) });
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

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        private Guid GetUserGuid()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
        }

        private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

        public class CreateFlagInput
        {
            [Required(ErrorMessage = "Key is required")]
            [StringLength(100, MinimumLength = 3, ErrorMessage = "Key must be between 3 and 100 characters")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Key can only contain letters, numbers, hyphens, and underscores")]
            public string? Key { get; set; }

            [Required(ErrorMessage = "Name is required")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
            public string? Name { get; set; }

            [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
            public string? Description { get; set; }
        }
    }
}
