using System.ComponentModel.DataAnnotations;
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

/// <summary>
/// Page model for managing VAT and tax configuration.
/// </summary>
[RequireRole(UserRole.Admin)]
public class VatConfigurationModel : PageModel
{
    private readonly ILogger<VatConfigurationModel> _logger;
    private readonly VatRuleService _vatRuleService;
    private readonly CategoryService _categoryService;

    // Common EU VAT country codes and their standard VAT rates for reference
    public static readonly IReadOnlyList<(string Code, string Name)> SupportedCountries = new List<(string, string)>
    {
        ("AT", "Austria"),
        ("BE", "Belgium"),
        ("BG", "Bulgaria"),
        ("CY", "Cyprus"),
        ("CZ", "Czech Republic"),
        ("DE", "Germany"),
        ("DK", "Denmark"),
        ("EE", "Estonia"),
        ("ES", "Spain"),
        ("FI", "Finland"),
        ("FR", "France"),
        ("GR", "Greece"),
        ("HR", "Croatia"),
        ("HU", "Hungary"),
        ("IE", "Ireland"),
        ("IT", "Italy"),
        ("LT", "Lithuania"),
        ("LU", "Luxembourg"),
        ("LV", "Latvia"),
        ("MT", "Malta"),
        ("NL", "Netherlands"),
        ("PL", "Poland"),
        ("PT", "Portugal"),
        ("RO", "Romania"),
        ("SE", "Sweden"),
        ("SI", "Slovenia"),
        ("SK", "Slovakia"),
        ("GB", "United Kingdom"),
        ("CH", "Switzerland"),
        ("NO", "Norway")
    };

    public VatConfigurationModel(
        ILogger<VatConfigurationModel> logger,
        VatRuleService vatRuleService,
        CategoryService categoryService)
    {
        _logger = logger;
        _vatRuleService = vatRuleService;
        _categoryService = categoryService;
    }

    public IReadOnlyCollection<VatRuleViewModel> Rules { get; private set; } = Array.Empty<VatRuleViewModel>();
    public IReadOnlyCollection<SelectListItem> CountryOptions { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; private set; } = Array.Empty<SelectListItem>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public CreateRuleInput NewRule { get; set; } = new();

    [BindProperty]
    public EditRuleInput EditRule { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        await LoadDataAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed VAT configuration, found {RuleCount} rules",
            GetUserId(),
            Rules.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        // Remove validation for category if it's a country-wide rule
        if (!NewRule.CategoryId.HasValue)
        {
            ModelState.Remove($"{nameof(NewRule)}.{nameof(NewRule.CategoryId)}");
        }

        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            await LoadDataAsync();
            ErrorMessage = "User authentication error.";
            return Page();
        }

        var command = new CreateVatRuleCommand(
            NewRule.CountryCode!,
            NewRule.Name!,
            NewRule.TaxRate,
            userId,
            NewRule.CategoryId,
            NewRule.Description,
            NewRule.EffectiveFrom,
            NewRule.EffectiveTo);

        var result = await _vatRuleService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} created VAT rule {RuleId}: {Rate}% for {Country}",
                GetUserId(),
                result.VatRule?.Id,
                result.VatRule?.TaxRate,
                result.VatRule?.CountryCode);

            return RedirectToPage(new { success = result.Message });
        }

        await LoadDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            await LoadDataAsync();
            ErrorMessage = "User authentication error.";
            return Page();
        }

        var command = new UpdateVatRuleCommand(
            EditRule.RuleId,
            EditRule.Name!,
            EditRule.TaxRate,
            userId,
            EditRule.CategoryId,
            EditRule.Description,
            EditRule.EffectiveFrom,
            EditRule.EffectiveTo,
            EditRule.ChangeReason);

        var result = await _vatRuleService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} updated VAT rule {RuleId}",
                GetUserId(),
                EditRule.RuleId);

            return RedirectToPage(new { success = result.Message });
        }

        await LoadDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid ruleId)
    {
        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "User authentication error." });
        }

        // First get the current rule to check its status
        var query = new GetVatRuleByIdQuery(ruleId);
        var rule = await _vatRuleService.HandleAsync(query);

        if (rule is null)
        {
            return RedirectToPage(new { error = "VAT rule not found." });
        }

        var result = rule.IsActive
            ? await _vatRuleService.HandleAsync(new DeactivateVatRuleCommand(ruleId, userId, null))
            : await _vatRuleService.HandleAsync(new ActivateVatRuleCommand(ruleId, userId, null));

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} toggled status for VAT rule {RuleId} to {IsActive}",
                GetUserId(),
                ruleId,
                result.VatRule?.IsActive);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid ruleId)
    {
        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "User authentication error." });
        }

        var command = new DeleteVatRuleCommand(ruleId, userId, null);
        var result = await _vatRuleService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} deleted VAT rule {RuleId}",
                GetUserId(),
                ruleId);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    private async Task LoadDataAsync()
    {
        // Load VAT rules
        var rules = await _vatRuleService.HandleAsync(new GetAllVatRulesQuery());
        Rules = rules.Select(r => new VatRuleViewModel
        {
            Id = r.Id,
            CountryCode = r.CountryCode,
            CountryName = r.CountryName,
            CategoryId = r.CategoryId,
            CategoryName = r.CategoryName,
            TaxRate = r.TaxRate,
            Name = r.Name,
            Description = r.Description,
            IsActive = r.IsActive,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            CreatedByUserId = r.CreatedByUserId,
            CreatedByUserName = r.CreatedByUserName,
            LastModifiedByUserId = r.LastModifiedByUserId,
            LastModifiedByUserName = r.LastModifiedByUserName,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        // Load country options
        var countryItems = new List<SelectListItem>
        {
            new SelectListItem("Select a country...", "")
        };
        countryItems.AddRange(SupportedCountries.Select(c => new SelectListItem(c.Name, c.Code)));
        CountryOptions = countryItems;

        // Load categories for dropdown
        var categories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery());
        var categoryItems = new List<SelectListItem>
        {
            new SelectListItem("All categories (country-wide)", "")
        };
        categoryItems.AddRange(categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())));
        CategoryOptions = categoryItems;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    private Guid GetUserIdGuid()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
    }

    public class CreateRuleInput
    {
        [Required(ErrorMessage = "Country is required")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be 2 characters")]
        public string? CountryCode { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Tax rate is required")]
        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100")]
        public decimal TaxRate { get; set; }

        public Guid? CategoryId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class EditRuleInput
    {
        [Required]
        public Guid RuleId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Tax rate is required")]
        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100")]
        public decimal TaxRate { get; set; }

        public Guid? CategoryId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Change reason cannot exceed 500 characters")]
        public string? ChangeReason { get; set; }
    }
}
