namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new VAT rule.
/// </summary>
public record CreateVatRuleCommand(
    string CountryCode,
    string Name,
    decimal TaxRate,
    Guid CreatedByUserId,
    Guid? CategoryId,
    string? Description,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// Command to update an existing VAT rule.
/// </summary>
public record UpdateVatRuleCommand(
    Guid RuleId,
    string Name,
    decimal TaxRate,
    Guid ModifiedByUserId,
    Guid? CategoryId,
    string? Description,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string? ChangeReason);

/// <summary>
/// Command to activate a VAT rule.
/// </summary>
public record ActivateVatRuleCommand(
    Guid RuleId,
    Guid ModifiedByUserId,
    string? ChangeReason);

/// <summary>
/// Command to deactivate a VAT rule.
/// </summary>
public record DeactivateVatRuleCommand(
    Guid RuleId,
    Guid ModifiedByUserId,
    string? ChangeReason);

/// <summary>
/// Command to delete a VAT rule.
/// </summary>
public record DeleteVatRuleCommand(
    Guid RuleId,
    Guid DeletedByUserId,
    string? ChangeReason);
