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
/// Page model for managing commission rules.
/// </summary>
[RequireRole(UserRole.Admin)]
public class CommissionRulesModel : PageModel
{
    private readonly ILogger<CommissionRulesModel> _logger;
    private readonly CommissionRuleService _commissionRuleService;
    private readonly CategoryService _categoryService;
    private readonly StoreService _storeService;

    public CommissionRulesModel(
        ILogger<CommissionRulesModel> logger,
        CommissionRuleService commissionRuleService,
        CategoryService categoryService,
        StoreService storeService)
    {
        _logger = logger;
        _commissionRuleService = commissionRuleService;
        _categoryService = categoryService;
        _storeService = storeService;
    }

    public IReadOnlyCollection<CommissionRuleViewModel> Rules { get; private set; } = Array.Empty<CommissionRuleViewModel>();
    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> StoreOptions { get; private set; } = Array.Empty<SelectListItem>();

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
            "Admin {UserId} viewed commission rules, found {RuleCount} rules",
            GetUserId(),
            Rules.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        // Remove validation for fields not required based on rule type
        if (NewRule.RuleType == CommissionRuleType.Global)
        {
            ModelState.Remove($"{nameof(NewRule)}.{nameof(NewRule.CategoryId)}");
            ModelState.Remove($"{nameof(NewRule)}.{nameof(NewRule.StoreId)}");
        }
        else if (NewRule.RuleType == CommissionRuleType.Category)
        {
            ModelState.Remove($"{nameof(NewRule)}.{nameof(NewRule.StoreId)}");
        }
        else if (NewRule.RuleType == CommissionRuleType.Seller)
        {
            ModelState.Remove($"{nameof(NewRule)}.{nameof(NewRule.CategoryId)}");
        }

        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var command = new CreateCommissionRuleCommand(
            NewRule.RuleType,
            NewRule.CategoryId,
            NewRule.StoreId,
            NewRule.CommissionRate,
            NewRule.Description,
            NewRule.EffectiveFrom,
            NewRule.EffectiveTo);

        var result = await _commissionRuleService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} created commission rule {RuleId}: {Rate}% ({RuleType})",
                GetUserId(),
                result.CommissionRule?.Id,
                result.CommissionRule?.CommissionRate,
                result.CommissionRule?.RuleType);

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

        var command = new UpdateCommissionRuleCommand(
            EditRule.RuleId,
            EditRule.CommissionRate,
            EditRule.Description,
            EditRule.EffectiveFrom,
            EditRule.EffectiveTo);

        var result = await _commissionRuleService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} updated commission rule {RuleId}",
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
        // First get the current rule to check its status
        var query = new GetCommissionRuleByIdQuery(ruleId);
        var rule = await _commissionRuleService.HandleAsync(query);

        if (rule is null)
        {
            return RedirectToPage(new { error = "Commission rule not found." });
        }

        var result = rule.IsActive
            ? await _commissionRuleService.HandleAsync(new DeactivateCommissionRuleCommand(ruleId))
            : await _commissionRuleService.HandleAsync(new ActivateCommissionRuleCommand(ruleId));

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} toggled status for commission rule {RuleId} to {IsActive}",
                GetUserId(),
                ruleId,
                result.CommissionRule?.IsActive);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid ruleId)
    {
        var command = new DeleteCommissionRuleCommand(ruleId);
        var result = await _commissionRuleService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} deleted commission rule {RuleId}",
                GetUserId(),
                ruleId);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    private async Task LoadDataAsync()
    {
        // Load commission rules
        var rules = await _commissionRuleService.HandleAsync(new GetAllCommissionRulesQuery());
        Rules = rules.Select(r => new CommissionRuleViewModel
        {
            Id = r.Id,
            RuleType = r.RuleType,
            CategoryId = r.CategoryId,
            CategoryName = r.CategoryName,
            StoreId = r.StoreId,
            StoreName = r.StoreName,
            CommissionRate = r.CommissionRate,
            Description = r.Description,
            IsActive = r.IsActive,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        // Load categories for dropdown
        var categories = await _categoryService.HandleAsync(new GetActiveCategoriesQuery());
        var categoryItems = new List<SelectListItem>
        {
            new SelectListItem("Select a category...", "")
        };
        categoryItems.AddRange(categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())));
        CategoryOptions = categoryItems;

        // Load stores for dropdown
        var stores = await _storeService.HandleAsync(new GetPublicStoresQuery());
        var storeItems = new List<SelectListItem>
        {
            new SelectListItem("Select a store...", "")
        };
        storeItems.AddRange(stores.Select(s => new SelectListItem(s.Name, s.Id.ToString())));
        StoreOptions = storeItems;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    public class CreateRuleInput
    {
        public CommissionRuleType RuleType { get; set; } = CommissionRuleType.Global;

        public Guid? CategoryId { get; set; }

        public Guid? StoreId { get; set; }

        [Required(ErrorMessage = "Commission rate is required")]
        [Range(0, 100, ErrorMessage = "Commission rate must be between 0 and 100")]
        public decimal CommissionRate { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class EditRuleInput
    {
        [Required]
        public Guid RuleId { get; set; }

        [Required(ErrorMessage = "Commission rate is required")]
        [Range(0, 100, ErrorMessage = "Commission rate must be between 0 and 100")]
        public decimal CommissionRate { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}
