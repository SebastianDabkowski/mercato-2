using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing commission rules.
/// </summary>
public sealed class CommissionRuleService
{
    private readonly ICommissionRuleRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IStoreRepository _storeRepository;

    public CommissionRuleService(
        ICommissionRuleRepository repository,
        ICategoryRepository categoryRepository,
        IStoreRepository storeRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);

        _repository = repository;
        _categoryRepository = categoryRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Gets all commission rules.
    /// </summary>
    public async Task<IReadOnlyCollection<CommissionRuleDto>> HandleAsync(
        GetAllCommissionRulesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rules = await _repository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(rules, cancellationToken);
    }

    /// <summary>
    /// Gets a commission rule by ID.
    /// </summary>
    public async Task<CommissionRuleDto?> HandleAsync(
        GetCommissionRuleByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rule = await _repository.GetByIdAsync(query.RuleId, cancellationToken);
        return rule is null ? null : await MapToDtoAsync(rule, cancellationToken);
    }

    /// <summary>
    /// Checks for conflicting commission rules.
    /// </summary>
    public async Task<IReadOnlyCollection<CommissionRuleConflictDto>> HandleAsync(
        CheckCommissionRuleConflictsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var overlapping = await _repository.GetOverlappingRulesAsync(
            query.RuleType,
            query.CategoryId,
            query.StoreId,
            query.EffectiveFrom,
            query.EffectiveTo,
            query.ExcludeRuleId,
            cancellationToken);

        return overlapping.Select(r => new CommissionRuleConflictDto(
            r.Id,
            r.RuleType,
            r.CommissionRate,
            r.EffectiveFrom,
            r.EffectiveTo,
            FormatConflictDescription(r)
        )).ToList();
    }

    /// <summary>
    /// Creates a new commission rule.
    /// </summary>
    public async Task<CommissionRuleResultDto> HandleAsync(
        CreateCommissionRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate inputs
        var validationErrors = new List<string>();

        if (command.CommissionRate < 0 || command.CommissionRate > 100)
        {
            validationErrors.Add("Commission rate must be between 0 and 100.");
        }

        if (command.EffectiveFrom.HasValue && command.EffectiveTo.HasValue &&
            command.EffectiveFrom.Value > command.EffectiveTo.Value)
        {
            validationErrors.Add("Effective from date must be before effective to date.");
        }

        // Validate category exists for category rules
        if (command.RuleType == CommissionRuleType.Category)
        {
            if (!command.CategoryId.HasValue)
            {
                validationErrors.Add("Category is required for category-specific rules.");
            }
            else
            {
                var category = await _categoryRepository.GetByIdAsync(command.CategoryId.Value, cancellationToken);
                if (category is null)
                {
                    validationErrors.Add("Selected category does not exist.");
                }
            }
        }

        // Validate store exists for seller rules
        if (command.RuleType == CommissionRuleType.Seller)
        {
            if (!command.StoreId.HasValue)
            {
                validationErrors.Add("Store is required for seller-specific rules.");
            }
            else
            {
                var store = await _storeRepository.GetByIdAsync(command.StoreId.Value, cancellationToken);
                if (store is null)
                {
                    validationErrors.Add("Selected store does not exist.");
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            return CommissionRuleResultDto.Failed(validationErrors);
        }

        // Check for conflicts
        var conflicts = await _repository.GetOverlappingRulesAsync(
            command.RuleType,
            command.CategoryId,
            command.StoreId,
            command.EffectiveFrom,
            command.EffectiveTo,
            excludeRuleId: null,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts.Select(c => FormatConflictDescription(c)).ToList();
            return CommissionRuleResultDto.Failed(new[]
            {
                $"This rule conflicts with {conflicts.Count} existing rule(s): {string.Join("; ", conflictMessages)}"
            });
        }

        // Create the rule
        CommissionRule rule;
        try
        {
            rule = command.RuleType switch
            {
                CommissionRuleType.Global => CommissionRule.CreateGlobalRule(
                    command.CommissionRate,
                    command.Description,
                    command.EffectiveFrom,
                    command.EffectiveTo),
                CommissionRuleType.Category => CommissionRule.CreateCategoryRule(
                    command.CategoryId!.Value,
                    command.CommissionRate,
                    command.Description,
                    command.EffectiveFrom,
                    command.EffectiveTo),
                CommissionRuleType.Seller => CommissionRule.CreateSellerRule(
                    command.StoreId!.Value,
                    command.CommissionRate,
                    command.Description,
                    command.EffectiveFrom,
                    command.EffectiveTo),
                _ => throw new ArgumentException($"Unknown rule type: {command.RuleType}")
            };
        }
        catch (ArgumentException ex)
        {
            return CommissionRuleResultDto.Failed(ex.Message);
        }

        await _repository.AddAsync(rule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return CommissionRuleResultDto.Succeeded(dto, "Commission rule created successfully.");
    }

    /// <summary>
    /// Updates an existing commission rule.
    /// </summary>
    public async Task<CommissionRuleResultDto> HandleAsync(
        UpdateCommissionRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return CommissionRuleResultDto.Failed("Commission rule not found.");
        }

        // Validate inputs
        var validationErrors = new List<string>();

        if (command.CommissionRate < 0 || command.CommissionRate > 100)
        {
            validationErrors.Add("Commission rate must be between 0 and 100.");
        }

        if (command.EffectiveFrom.HasValue && command.EffectiveTo.HasValue &&
            command.EffectiveFrom.Value > command.EffectiveTo.Value)
        {
            validationErrors.Add("Effective from date must be before effective to date.");
        }

        if (validationErrors.Count > 0)
        {
            return CommissionRuleResultDto.Failed(validationErrors);
        }

        // Check for conflicts (exclude current rule)
        var conflicts = await _repository.GetOverlappingRulesAsync(
            rule.RuleType,
            rule.CategoryId,
            rule.StoreId,
            command.EffectiveFrom,
            command.EffectiveTo,
            excludeRuleId: rule.Id,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts.Select(c => FormatConflictDescription(c)).ToList();
            return CommissionRuleResultDto.Failed(new[]
            {
                $"This update would conflict with {conflicts.Count} existing rule(s): {string.Join("; ", conflictMessages)}"
            });
        }

        // Update the rule
        try
        {
            rule.UpdateCommissionRate(command.CommissionRate);
            rule.UpdateDescription(command.Description);
            rule.UpdateEffectiveDates(command.EffectiveFrom, command.EffectiveTo);
        }
        catch (ArgumentException ex)
        {
            return CommissionRuleResultDto.Failed(ex.Message);
        }

        await _repository.UpdateAsync(rule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return CommissionRuleResultDto.Succeeded(dto, "Commission rule updated successfully.");
    }

    /// <summary>
    /// Activates a commission rule.
    /// </summary>
    public async Task<CommissionRuleResultDto> HandleAsync(
        ActivateCommissionRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return CommissionRuleResultDto.Failed("Commission rule not found.");
        }

        if (rule.IsActive)
        {
            return CommissionRuleResultDto.Failed("Commission rule is already active.");
        }

        // Check for conflicts when activating
        var conflicts = await _repository.GetOverlappingRulesAsync(
            rule.RuleType,
            rule.CategoryId,
            rule.StoreId,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            excludeRuleId: rule.Id,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflictMessages = conflicts.Select(c => FormatConflictDescription(c)).ToList();
            return CommissionRuleResultDto.Failed(new[]
            {
                $"Activating this rule would conflict with {conflicts.Count} existing rule(s): {string.Join("; ", conflictMessages)}"
            });
        }

        rule.Activate();
        await _repository.UpdateAsync(rule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return CommissionRuleResultDto.Succeeded(dto, "Commission rule activated successfully.");
    }

    /// <summary>
    /// Deactivates a commission rule.
    /// </summary>
    public async Task<CommissionRuleResultDto> HandleAsync(
        DeactivateCommissionRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return CommissionRuleResultDto.Failed("Commission rule not found.");
        }

        if (!rule.IsActive)
        {
            return CommissionRuleResultDto.Failed("Commission rule is already inactive.");
        }

        rule.Deactivate();
        await _repository.UpdateAsync(rule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var dto = await MapToDtoAsync(rule, cancellationToken);
        return CommissionRuleResultDto.Succeeded(dto, "Commission rule deactivated successfully.");
    }

    /// <summary>
    /// Deletes a commission rule.
    /// </summary>
    public async Task<DeleteCommissionRuleResultDto> HandleAsync(
        DeleteCommissionRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return DeleteCommissionRuleResultDto.Failed("Commission rule not found.");
        }

        await _repository.DeleteAsync(rule, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return DeleteCommissionRuleResultDto.Succeeded("Commission rule deleted successfully.");
    }

    private async Task<CommissionRuleDto> MapToDtoAsync(
        CommissionRule rule,
        CancellationToken cancellationToken)
    {
        string? categoryName = null;
        string? storeName = null;

        if (rule.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(rule.CategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        if (rule.StoreId.HasValue)
        {
            var store = await _storeRepository.GetByIdAsync(rule.StoreId.Value, cancellationToken);
            storeName = store?.Name;
        }

        return new CommissionRuleDto(
            rule.Id,
            rule.RuleType,
            rule.CategoryId,
            categoryName,
            rule.StoreId,
            storeName,
            rule.CommissionRate,
            rule.Description,
            rule.IsActive,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.CreatedAt,
            rule.UpdatedAt);
    }

    private async Task<IReadOnlyCollection<CommissionRuleDto>> MapToDtosAsync(
        IReadOnlyList<CommissionRule> rules,
        CancellationToken cancellationToken)
    {
        if (rules.Count == 0)
        {
            return Array.Empty<CommissionRuleDto>();
        }

        // Batch load categories and stores
        var categoryIds = rules.Where(r => r.CategoryId.HasValue).Select(r => r.CategoryId!.Value).Distinct().ToHashSet();
        var storeIds = rules.Where(r => r.StoreId.HasValue).Select(r => r.StoreId!.Value).Distinct().ToList();

        // Load all categories once and filter locally to avoid N+1 queries
        var categories = new Dictionary<Guid, string>();
        if (categoryIds.Count > 0)
        {
            var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);
            foreach (var category in allCategories.Where(c => categoryIds.Contains(c.Id)))
            {
                categories[category.Id] = category.Name;
            }
        }

        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeNames = stores.ToDictionary(s => s.Id, s => s.Name);

        return rules.Select(r => new CommissionRuleDto(
            r.Id,
            r.RuleType,
            r.CategoryId,
            r.CategoryId.HasValue ? categories.GetValueOrDefault(r.CategoryId.Value) : null,
            r.StoreId,
            r.StoreId.HasValue ? storeNames.GetValueOrDefault(r.StoreId.Value) : null,
            r.CommissionRate,
            r.Description,
            r.IsActive,
            r.EffectiveFrom,
            r.EffectiveTo,
            r.CreatedAt,
            r.UpdatedAt
        )).ToList();
    }

    private static string FormatConflictDescription(CommissionRule rule)
    {
        var dateRange = (rule.EffectiveFrom, rule.EffectiveTo) switch
        {
            (null, null) => "always effective",
            (DateTime from, null) => $"from {from:MMM d, yyyy}",
            (null, DateTime to) => $"until {to:MMM d, yyyy}",
            (DateTime from, DateTime to) => $"{from:MMM d, yyyy} - {to:MMM d, yyyy}"
        };
        return $"{rule.CommissionRate}% ({dateRange})";
    }
}
