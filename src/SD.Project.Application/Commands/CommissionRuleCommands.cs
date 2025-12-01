using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new commission rule.
/// </summary>
public sealed record CreateCommissionRuleCommand(
    CommissionRuleType RuleType,
    Guid? CategoryId,
    Guid? StoreId,
    decimal CommissionRate,
    string? Description,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// Command to update an existing commission rule.
/// </summary>
public sealed record UpdateCommissionRuleCommand(
    Guid RuleId,
    decimal CommissionRate,
    string? Description,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// Command to deactivate a commission rule.
/// </summary>
public sealed record DeactivateCommissionRuleCommand(Guid RuleId);

/// <summary>
/// Command to activate a commission rule.
/// </summary>
public sealed record ActivateCommissionRuleCommand(Guid RuleId);

/// <summary>
/// Command to delete a commission rule.
/// </summary>
public sealed record DeleteCommissionRuleCommand(Guid RuleId);
