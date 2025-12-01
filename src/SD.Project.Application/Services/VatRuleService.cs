using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing VAT rules.
/// </summary>
public sealed class VatRuleService
{
    private readonly IVatRuleRepository _repository;
    private readonly IVatRuleHistoryRepository _historyRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUserRepository _userRepository;

    // Common country names for ISO codes
    private static readonly Dictionary<string, string> CountryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AT"] = "Austria",
        ["BE"] = "Belgium",
        ["BG"] = "Bulgaria",
        ["CY"] = "Cyprus",
        ["CZ"] = "Czech Republic",
        ["DE"] = "Germany",
        ["DK"] = "Denmark",
        ["EE"] = "Estonia",
        ["ES"] = "Spain",
        ["FI"] = "Finland",
        ["FR"] = "France",
        ["GR"] = "Greece",
        ["HR"] = "Croatia",
        ["HU"] = "Hungary",
        ["IE"] = "Ireland",
        ["IT"] = "Italy",
        ["LT"] = "Lithuania",
        ["LU"] = "Luxembourg",
        ["LV"] = "Latvia",
        ["MT"] = "Malta",
        ["NL"] = "Netherlands",
        ["PL"] = "Poland",
        ["PT"] = "Portugal",
        ["RO"] = "Romania",
        ["SE"] = "Sweden",
        ["SI"] = "Slovenia",
        ["SK"] = "Slovakia",
        ["GB"] = "United Kingdom",
        ["US"] = "United States",
        ["CH"] = "Switzerland",
        ["NO"] = "Norway"
    };

    public VatRuleService(
        IVatRuleRepository repository,
        IVatRuleHistoryRepository historyRepository,
        ICategoryRepository categoryRepository,
        IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(historyRepository);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(userRepository);

        _repository = repository;
        _historyRepository = historyRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Gets all VAT rules.
    /// </summary>
    public async Task<IReadOnlyCollection<VatRuleDto>> HandleAsync(
        GetAllVatRulesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rules = await _repository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(rules, cancellationToken);
    }

    /// <summary>
    /// Gets a VAT rule by ID.
    /// </summary>
    public async Task<VatRuleDto?> HandleAsync(
        GetVatRuleByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rule = await _repository.GetByIdAsync(query.RuleId, cancellationToken);
        return rule is null ? null : await MapToDtoAsync(rule, cancellationToken);
    }

    /// <summary>
    /// Gets VAT rules by country.
    /// </summary>
    public async Task<IReadOnlyCollection<VatRuleDto>> HandleAsync(
        GetVatRulesByCountryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rules = await _repository.GetByCountryAsync(query.CountryCode, cancellationToken);
        return await MapToDtosAsync(rules, cancellationToken);
    }

    /// <summary>
    /// Gets the effective VAT rule for a country and optional category.
    /// </summary>
    public async Task<VatRuleDto?> HandleAsync(
        GetEffectiveVatRuleQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rule = await _repository.GetEffectiveRuleAsync(
            query.CountryCode,
            query.CategoryId,
            query.EffectiveDate,
            cancellationToken);

        return rule is null ? null : await MapToDtoAsync(rule, cancellationToken);
    }

    /// <summary>
    /// Gets the history of a VAT rule.
    /// </summary>
    public async Task<IReadOnlyCollection<VatRuleHistoryDto>> HandleAsync(
        GetVatRuleHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var history = await _historyRepository.GetByVatRuleIdAsync(query.VatRuleId, cancellationToken);
        return await MapHistoryToDtosAsync(history, cancellationToken);
    }

    /// <summary>
    /// Gets all VAT rule history entries.
    /// </summary>
    public async Task<IReadOnlyCollection<VatRuleHistoryDto>> HandleAsync(
        GetAllVatRuleHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var history = await _historyRepository.GetAllAsync(query.CountryCode, cancellationToken);
        return await MapHistoryToDtosAsync(history, cancellationToken);
    }

    /// <summary>
    /// Checks for conflicting VAT rules.
    /// </summary>
    public async Task<IReadOnlyCollection<VatRuleConflictDto>> HandleAsync(
        CheckVatRuleConflictsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var overlapping = await _repository.GetOverlappingRulesAsync(
            query.CountryCode,
            query.CategoryId,
            query.EffectiveFrom,
            query.EffectiveTo,
            query.ExcludeRuleId,
            cancellationToken);

        return overlapping.Select(r => new VatRuleConflictDto(
            r.Id,
            r.CountryCode,
            r.CategoryId,
            r.TaxRate,
            r.EffectiveFrom,
            r.EffectiveTo,
            FormatConflictDescription(r)
        )).ToList();
    }

    /// <summary>
    /// Gets distinct country codes with VAT rules.
    /// </summary>
    public async Task<IReadOnlyCollection<string>> HandleAsync(
        GetVatRuleCountryCodesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _repository.GetDistinctCountryCodesAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new VAT rule.
    /// </summary>
    public async Task<VatRuleResultDto> HandleAsync(
        CreateVatRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate inputs
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.CountryCode))
        {
            validationErrors.Add("Country code is required.");
        }
        else if (command.CountryCode.Length != 2)
        {
            validationErrors.Add("Country code must be a 2-letter ISO code.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            validationErrors.Add("Name is required.");
        }

        if (command.TaxRate < 0 || command.TaxRate > 100)
        {
            validationErrors.Add("Tax rate must be between 0 and 100.");
        }

        if (command.EffectiveFrom.HasValue && command.EffectiveTo.HasValue &&
            command.EffectiveFrom.Value > command.EffectiveTo.Value)
        {
            validationErrors.Add("Effective from date must be before effective to date.");
        }

        // Validate category exists if specified
        if (command.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId.Value, cancellationToken);
            if (category is null)
            {
                validationErrors.Add("Selected category does not exist.");
            }
        }

        // Validate creator user exists
        var creatorUser = await _userRepository.GetByIdAsync(command.CreatedByUserId, cancellationToken);
        if (creatorUser is null)
        {
            validationErrors.Add("Creator user not found.");
        }

        if (validationErrors.Count > 0)
        {
            return VatRuleResultDto.Failed(validationErrors);
        }

        // Check for conflicts
        var conflicts = await _repository.GetOverlappingRulesAsync(
            command.CountryCode,
            command.CategoryId,
            command.EffectiveFrom,
            command.EffectiveTo,
            excludeRuleId: null,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts.Select(c => FormatConflictDescription(c)).ToList();
            return VatRuleResultDto.Failed(new[]
            {
                $"This rule conflicts with {conflicts.Count} existing rule(s): {string.Join("; ", conflictMessages)}"
            });
        }

        // Create the rule
        VatRule rule;
        try
        {
            rule = new VatRule(
                command.CountryCode,
                command.Name,
                command.TaxRate,
                command.CreatedByUserId,
                command.CategoryId,
                command.Description,
                command.EffectiveFrom,
                command.EffectiveTo);
        }
        catch (ArgumentException ex)
        {
            return VatRuleResultDto.Failed(ex.Message);
        }

        await _repository.AddAsync(rule, cancellationToken);

        // Create history entry
        var creatorName = creatorUser!.FirstName != null && creatorUser.LastName != null
            ? $"{creatorUser.FirstName} {creatorUser.LastName}"
            : creatorUser.Email.Value;

        var historyEntry = VatRuleHistory.FromVatRule(
            rule,
            VatRuleChangeType.Created,
            command.CreatedByUserId,
            creatorName,
            "Initial creation");

        await _historyRepository.AddAsync(historyEntry, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return VatRuleResultDto.Succeeded(dto, "VAT rule created successfully.");
    }

    /// <summary>
    /// Updates an existing VAT rule.
    /// </summary>
    public async Task<VatRuleResultDto> HandleAsync(
        UpdateVatRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return VatRuleResultDto.Failed("VAT rule not found.");
        }

        // Validate inputs
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            validationErrors.Add("Name is required.");
        }

        if (command.TaxRate < 0 || command.TaxRate > 100)
        {
            validationErrors.Add("Tax rate must be between 0 and 100.");
        }

        if (command.EffectiveFrom.HasValue && command.EffectiveTo.HasValue &&
            command.EffectiveFrom.Value > command.EffectiveTo.Value)
        {
            validationErrors.Add("Effective from date must be before effective to date.");
        }

        // Validate category exists if specified
        if (command.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId.Value, cancellationToken);
            if (category is null)
            {
                validationErrors.Add("Selected category does not exist.");
            }
        }

        // Validate modifier user exists
        var modifierUser = await _userRepository.GetByIdAsync(command.ModifiedByUserId, cancellationToken);
        if (modifierUser is null)
        {
            validationErrors.Add("Modifier user not found.");
        }

        if (validationErrors.Count > 0)
        {
            return VatRuleResultDto.Failed(validationErrors);
        }

        // Check for conflicts (exclude current rule)
        var conflicts = await _repository.GetOverlappingRulesAsync(
            rule.CountryCode,
            command.CategoryId,
            command.EffectiveFrom,
            command.EffectiveTo,
            excludeRuleId: rule.Id,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts.Select(c => FormatConflictDescription(c)).ToList();
            return VatRuleResultDto.Failed(new[]
            {
                $"This update would conflict with {conflicts.Count} existing rule(s): {string.Join("; ", conflictMessages)}"
            });
        }

        // Update the rule
        try
        {
            rule.UpdateName(command.Name, command.ModifiedByUserId);
            rule.UpdateTaxRate(command.TaxRate, command.ModifiedByUserId);
            rule.UpdateDescription(command.Description, command.ModifiedByUserId);
            rule.UpdateCategory(command.CategoryId, command.ModifiedByUserId);
            rule.UpdateEffectiveDates(command.EffectiveFrom, command.EffectiveTo, command.ModifiedByUserId);
        }
        catch (ArgumentException ex)
        {
            return VatRuleResultDto.Failed(ex.Message);
        }

        await _repository.UpdateAsync(rule, cancellationToken);

        // Create history entry
        var modifierName = modifierUser!.FirstName != null && modifierUser.LastName != null
            ? $"{modifierUser.FirstName} {modifierUser.LastName}"
            : modifierUser.Email.Value;

        var historyEntry = VatRuleHistory.FromVatRule(
            rule,
            VatRuleChangeType.Updated,
            command.ModifiedByUserId,
            modifierName,
            command.ChangeReason);

        await _historyRepository.AddAsync(historyEntry, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return VatRuleResultDto.Succeeded(dto, "VAT rule updated successfully.");
    }

    /// <summary>
    /// Activates a VAT rule.
    /// </summary>
    public async Task<VatRuleResultDto> HandleAsync(
        ActivateVatRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return VatRuleResultDto.Failed("VAT rule not found.");
        }

        if (rule.IsActive)
        {
            return VatRuleResultDto.Failed("VAT rule is already active.");
        }

        var modifierUser = await _userRepository.GetByIdAsync(command.ModifiedByUserId, cancellationToken);
        if (modifierUser is null)
        {
            return VatRuleResultDto.Failed("Modifier user not found.");
        }

        // Check for conflicts when activating
        var conflicts = await _repository.GetOverlappingRulesAsync(
            rule.CountryCode,
            rule.CategoryId,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            excludeRuleId: rule.Id,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts.Select(c => FormatConflictDescription(c)).ToList();
            return VatRuleResultDto.Failed(new[]
            {
                $"Activating this rule would conflict with {conflicts.Count} existing rule(s): {string.Join("; ", conflictMessages)}"
            });
        }

        rule.Activate(command.ModifiedByUserId);
        await _repository.UpdateAsync(rule, cancellationToken);

        // Create history entry
        var modifierName = modifierUser.FirstName != null && modifierUser.LastName != null
            ? $"{modifierUser.FirstName} {modifierUser.LastName}"
            : modifierUser.Email.Value;

        var historyEntry = VatRuleHistory.FromVatRule(
            rule,
            VatRuleChangeType.Activated,
            command.ModifiedByUserId,
            modifierName,
            command.ChangeReason);

        await _historyRepository.AddAsync(historyEntry, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return VatRuleResultDto.Succeeded(dto, "VAT rule activated successfully.");
    }

    /// <summary>
    /// Deactivates a VAT rule.
    /// </summary>
    public async Task<VatRuleResultDto> HandleAsync(
        DeactivateVatRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return VatRuleResultDto.Failed("VAT rule not found.");
        }

        if (!rule.IsActive)
        {
            return VatRuleResultDto.Failed("VAT rule is already inactive.");
        }

        var modifierUser = await _userRepository.GetByIdAsync(command.ModifiedByUserId, cancellationToken);
        if (modifierUser is null)
        {
            return VatRuleResultDto.Failed("Modifier user not found.");
        }

        rule.Deactivate(command.ModifiedByUserId);
        await _repository.UpdateAsync(rule, cancellationToken);

        // Create history entry
        var modifierName = modifierUser.FirstName != null && modifierUser.LastName != null
            ? $"{modifierUser.FirstName} {modifierUser.LastName}"
            : modifierUser.Email.Value;

        var historyEntry = VatRuleHistory.FromVatRule(
            rule,
            VatRuleChangeType.Deactivated,
            command.ModifiedByUserId,
            modifierName,
            command.ChangeReason);

        await _historyRepository.AddAsync(historyEntry, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return VatRuleResultDto.Succeeded(dto, "VAT rule deactivated successfully.");
    }

    /// <summary>
    /// Deletes a VAT rule.
    /// </summary>
    public async Task<DeleteVatRuleResultDto> HandleAsync(
        DeleteVatRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return DeleteVatRuleResultDto.Failed("VAT rule not found.");
        }

        var deletedByUser = await _userRepository.GetByIdAsync(command.DeletedByUserId, cancellationToken);
        if (deletedByUser is null)
        {
            return DeleteVatRuleResultDto.Failed("User not found.");
        }

        // Create history entry before deletion
        var userName = deletedByUser.FirstName != null && deletedByUser.LastName != null
            ? $"{deletedByUser.FirstName} {deletedByUser.LastName}"
            : deletedByUser.Email.Value;

        var historyEntry = VatRuleHistory.FromVatRule(
            rule,
            VatRuleChangeType.Deleted,
            command.DeletedByUserId,
            userName,
            command.ChangeReason);

        await _historyRepository.AddAsync(historyEntry, cancellationToken);

        await _repository.DeleteAsync(rule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return DeleteVatRuleResultDto.Succeeded("VAT rule deleted successfully.");
    }

    private async Task<VatRuleDto> MapToDtoAsync(
        VatRule rule,
        CancellationToken cancellationToken)
    {
        string? categoryName = null;
        string? createdByUserName = null;
        string? lastModifiedByUserName = null;

        if (rule.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(rule.CategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        var createdByUser = await _userRepository.GetByIdAsync(rule.CreatedByUserId, cancellationToken);
        if (createdByUser != null)
        {
            createdByUserName = createdByUser.FirstName != null && createdByUser.LastName != null
                ? $"{createdByUser.FirstName} {createdByUser.LastName}"
                : createdByUser.Email.Value;
        }

        if (rule.LastModifiedByUserId.HasValue)
        {
            var lastModifiedByUser = await _userRepository.GetByIdAsync(rule.LastModifiedByUserId.Value, cancellationToken);
            if (lastModifiedByUser != null)
            {
                lastModifiedByUserName = lastModifiedByUser.FirstName != null && lastModifiedByUser.LastName != null
                    ? $"{lastModifiedByUser.FirstName} {lastModifiedByUser.LastName}"
                    : lastModifiedByUser.Email.Value;
            }
        }

        return new VatRuleDto(
            rule.Id,
            rule.CountryCode,
            GetCountryName(rule.CountryCode),
            rule.CategoryId,
            categoryName,
            rule.TaxRate,
            rule.Name,
            rule.Description,
            rule.IsActive,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.CreatedByUserId,
            createdByUserName,
            rule.LastModifiedByUserId,
            lastModifiedByUserName,
            rule.CreatedAt,
            rule.UpdatedAt);
    }

    private async Task<IReadOnlyCollection<VatRuleDto>> MapToDtosAsync(
        IReadOnlyList<VatRule> rules,
        CancellationToken cancellationToken)
    {
        if (rules.Count == 0)
        {
            return Array.Empty<VatRuleDto>();
        }

        // Batch load categories and users
        var categoryIds = rules.Where(r => r.CategoryId.HasValue).Select(r => r.CategoryId!.Value).Distinct().ToHashSet();
        var userIds = rules.Select(r => r.CreatedByUserId)
            .Concat(rules.Where(r => r.LastModifiedByUserId.HasValue).Select(r => r.LastModifiedByUserId!.Value))
            .Distinct()
            .ToList();

        // Load all categories once and filter locally
        var categories = new Dictionary<Guid, string>();
        if (categoryIds.Count > 0)
        {
            var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);
            foreach (var category in allCategories.Where(c => categoryIds.Contains(c.Id)))
            {
                categories[category.Id] = category.Name;
            }
        }

        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(
            u => u.Id,
            u => u.FirstName != null && u.LastName != null
                ? $"{u.FirstName} {u.LastName}"
                : u.Email.Value);

        return rules.Select(r => new VatRuleDto(
            r.Id,
            r.CountryCode,
            GetCountryName(r.CountryCode),
            r.CategoryId,
            r.CategoryId.HasValue ? categories.GetValueOrDefault(r.CategoryId.Value) : null,
            r.TaxRate,
            r.Name,
            r.Description,
            r.IsActive,
            r.EffectiveFrom,
            r.EffectiveTo,
            r.CreatedByUserId,
            userNames.GetValueOrDefault(r.CreatedByUserId),
            r.LastModifiedByUserId,
            r.LastModifiedByUserId.HasValue ? userNames.GetValueOrDefault(r.LastModifiedByUserId.Value) : null,
            r.CreatedAt,
            r.UpdatedAt
        )).ToList();
    }

    private async Task<IReadOnlyCollection<VatRuleHistoryDto>> MapHistoryToDtosAsync(
        IReadOnlyList<VatRuleHistory> history,
        CancellationToken cancellationToken)
    {
        if (history.Count == 0)
        {
            return Array.Empty<VatRuleHistoryDto>();
        }

        // Batch load categories
        var categoryIds = history.Where(h => h.CategoryId.HasValue).Select(h => h.CategoryId!.Value).Distinct().ToHashSet();

        var categories = new Dictionary<Guid, string>();
        if (categoryIds.Count > 0)
        {
            var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);
            foreach (var category in allCategories.Where(c => categoryIds.Contains(c.Id)))
            {
                categories[category.Id] = category.Name;
            }
        }

        return history.Select(h => new VatRuleHistoryDto(
            h.Id,
            h.VatRuleId,
            h.ChangeType,
            h.CountryCode,
            GetCountryName(h.CountryCode),
            h.CategoryId,
            h.CategoryId.HasValue ? categories.GetValueOrDefault(h.CategoryId.Value) : null,
            h.TaxRate,
            h.Name,
            h.Description,
            h.IsActive,
            h.EffectiveFrom,
            h.EffectiveTo,
            h.ChangedByUserId,
            h.ChangedByUserName,
            h.ChangeReason,
            h.CreatedAt
        )).ToList();
    }

    private static string GetCountryName(string countryCode)
    {
        return CountryNames.TryGetValue(countryCode.ToUpperInvariant(), out var name) ? name : countryCode;
    }

    private static string FormatConflictDescription(VatRule rule)
    {
        var dateRange = (rule.EffectiveFrom, rule.EffectiveTo) switch
        {
            (null, null) => "always effective",
            (DateTime from, null) => $"from {from:MMM d, yyyy}",
            (null, DateTime to) => $"until {to:MMM d, yyyy}",
            (DateTime from, DateTime to) => $"{from:MMM d, yyyy} - {to:MMM d, yyyy}"
        };
        return $"{rule.Name} - {rule.TaxRate}% ({dateRange})";
    }
}
