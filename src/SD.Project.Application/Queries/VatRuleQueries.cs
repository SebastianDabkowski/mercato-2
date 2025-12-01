namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all VAT rules.
/// </summary>
public record GetAllVatRulesQuery;

/// <summary>
/// Query to get a VAT rule by ID.
/// </summary>
public record GetVatRuleByIdQuery(Guid RuleId);

/// <summary>
/// Query to get VAT rules by country.
/// </summary>
public record GetVatRulesByCountryQuery(string CountryCode);

/// <summary>
/// Query to get the effective VAT rule for a country and optional category.
/// </summary>
public record GetEffectiveVatRuleQuery(
    string CountryCode,
    Guid? CategoryId,
    DateTime? EffectiveDate);

/// <summary>
/// Query to get the history of a VAT rule.
/// </summary>
public record GetVatRuleHistoryQuery(Guid VatRuleId);

/// <summary>
/// Query to get all VAT rule history entries with optional country filter.
/// </summary>
public record GetAllVatRuleHistoryQuery(string? CountryCode = null);

/// <summary>
/// Query to check for conflicting VAT rules.
/// </summary>
public record CheckVatRuleConflictsQuery(
    string CountryCode,
    Guid? CategoryId,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    Guid? ExcludeRuleId = null);

/// <summary>
/// Query to get distinct country codes with VAT rules.
/// </summary>
public record GetVatRuleCountryCodesQuery;
