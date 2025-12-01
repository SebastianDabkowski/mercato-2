using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for viewing VAT rule change history.
/// </summary>
[RequireRole(UserRole.Admin)]
public class VatHistoryModel : PageModel
{
    private readonly ILogger<VatHistoryModel> _logger;
    private readonly VatRuleService _vatRuleService;

    public VatHistoryModel(
        ILogger<VatHistoryModel> logger,
        VatRuleService vatRuleService)
    {
        _logger = logger;
        _vatRuleService = vatRuleService;
    }

    public IReadOnlyCollection<VatRuleHistoryViewModel> HistoryEntries { get; private set; } = Array.Empty<VatRuleHistoryViewModel>();
    public VatRuleViewModel? SelectedRule { get; private set; }
    public Guid? RuleIdFilter { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? ruleId = null)
    {
        RuleIdFilter = ruleId;

        if (ruleId.HasValue)
        {
            // Load specific rule and its history
            var rule = await _vatRuleService.HandleAsync(new GetVatRuleByIdQuery(ruleId.Value));
            if (rule != null)
            {
                SelectedRule = new VatRuleViewModel
                {
                    Id = rule.Id,
                    CountryCode = rule.CountryCode,
                    CountryName = rule.CountryName,
                    CategoryId = rule.CategoryId,
                    CategoryName = rule.CategoryName,
                    TaxRate = rule.TaxRate,
                    Name = rule.Name,
                    Description = rule.Description,
                    IsActive = rule.IsActive,
                    EffectiveFrom = rule.EffectiveFrom,
                    EffectiveTo = rule.EffectiveTo,
                    CreatedByUserId = rule.CreatedByUserId,
                    CreatedByUserName = rule.CreatedByUserName,
                    LastModifiedByUserId = rule.LastModifiedByUserId,
                    LastModifiedByUserName = rule.LastModifiedByUserName,
                    CreatedAt = rule.CreatedAt,
                    UpdatedAt = rule.UpdatedAt
                };
            }

            var history = await _vatRuleService.HandleAsync(new GetVatRuleHistoryQuery(ruleId.Value));
            HistoryEntries = history.Select(h => new VatRuleHistoryViewModel
            {
                Id = h.Id,
                VatRuleId = h.VatRuleId,
                ChangeType = h.ChangeType,
                CountryCode = h.CountryCode,
                CountryName = h.CountryName,
                CategoryId = h.CategoryId,
                CategoryName = h.CategoryName,
                TaxRate = h.TaxRate,
                Name = h.Name,
                Description = h.Description,
                IsActive = h.IsActive,
                EffectiveFrom = h.EffectiveFrom,
                EffectiveTo = h.EffectiveTo,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByUserName = h.ChangedByUserName,
                ChangeReason = h.ChangeReason,
                CreatedAt = h.CreatedAt
            }).ToList();
        }
        else
        {
            // Load all history
            var history = await _vatRuleService.HandleAsync(new GetAllVatRuleHistoryQuery());
            HistoryEntries = history.Select(h => new VatRuleHistoryViewModel
            {
                Id = h.Id,
                VatRuleId = h.VatRuleId,
                ChangeType = h.ChangeType,
                CountryCode = h.CountryCode,
                CountryName = h.CountryName,
                CategoryId = h.CategoryId,
                CategoryName = h.CategoryName,
                TaxRate = h.TaxRate,
                Name = h.Name,
                Description = h.Description,
                IsActive = h.IsActive,
                EffectiveFrom = h.EffectiveFrom,
                EffectiveTo = h.EffectiveTo,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByUserName = h.ChangedByUserName,
                ChangeReason = h.ChangeReason,
                CreatedAt = h.CreatedAt
            }).ToList();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        _logger.LogInformation(
            "Admin {UserId} viewed VAT history, found {EntryCount} entries{RuleFilter}",
            userId,
            HistoryEntries.Count,
            ruleId.HasValue ? $" for rule {ruleId}" : "");

        return Page();
    }
}
