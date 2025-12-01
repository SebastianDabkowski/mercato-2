namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all commission rules.
/// </summary>
public sealed record GetAllCommissionRulesQuery;

/// <summary>
/// Query to get a commission rule by ID.
/// </summary>
public sealed record GetCommissionRuleByIdQuery(Guid RuleId);

/// <summary>
/// Query to check for conflicting commission rules.
/// </summary>
public sealed record CheckCommissionRuleConflictsQuery(
    Guid? ExcludeRuleId,
    SD.Project.Domain.Entities.CommissionRuleType RuleType,
    Guid? CategoryId,
    Guid? StoreId,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);
