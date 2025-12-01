using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for VAT rule information.
/// </summary>
public record VatRuleDto(
    Guid Id,
    string CountryCode,
    string CountryName,
    Guid? CategoryId,
    string? CategoryName,
    decimal TaxRate,
    string Name,
    string? Description,
    bool IsActive,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    Guid CreatedByUserId,
    string? CreatedByUserName,
    Guid? LastModifiedByUserId,
    string? LastModifiedByUserName,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets the formatted tax rate (e.g., "23%").
    /// </summary>
    public string FormattedRate => $"{TaxRate}%";

    /// <summary>
    /// Checks if this rule is currently effective.
    /// </summary>
    public bool IsCurrentlyEffective =>
        IsActive &&
        (!EffectiveFrom.HasValue || EffectiveFrom.Value <= DateTime.UtcNow) &&
        (!EffectiveTo.HasValue || EffectiveTo.Value >= DateTime.UtcNow);

    /// <summary>
    /// Checks if this rule has a future effective date.
    /// </summary>
    public bool IsFutureDated => EffectiveFrom.HasValue && EffectiveFrom.Value > DateTime.UtcNow;
}

/// <summary>
/// Data transfer object for VAT rule history entry.
/// </summary>
public record VatRuleHistoryDto(
    Guid Id,
    Guid VatRuleId,
    VatRuleChangeType ChangeType,
    string CountryCode,
    string CountryName,
    Guid? CategoryId,
    string? CategoryName,
    decimal TaxRate,
    string Name,
    string? Description,
    bool IsActive,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    Guid ChangedByUserId,
    string ChangedByUserName,
    string? ChangeReason,
    DateTime CreatedAt)
{
    /// <summary>
    /// Gets a human-readable description of the change type.
    /// </summary>
    public string ChangeTypeDescription => ChangeType switch
    {
        VatRuleChangeType.Created => "Created",
        VatRuleChangeType.Updated => "Updated",
        VatRuleChangeType.Activated => "Activated",
        VatRuleChangeType.Deactivated => "Deactivated",
        VatRuleChangeType.Deleted => "Deleted",
        _ => "Unknown"
    };
}

/// <summary>
/// Result DTO for VAT rule operations.
/// </summary>
public record VatRuleResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public VatRuleDto? VatRule { get; init; }

    public static VatRuleResultDto Succeeded(VatRuleDto vatRule, string message) => new()
    {
        Success = true,
        Message = message,
        VatRule = vatRule
    };

    public static VatRuleResultDto Failed(string error) => new()
    {
        Success = false,
        Errors = new[] { error }
    };

    public static VatRuleResultDto Failed(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Result DTO for delete VAT rule operations.
/// </summary>
public record DeleteVatRuleResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static DeleteVatRuleResultDto Succeeded(string message) => new()
    {
        Success = true,
        Message = message
    };

    public static DeleteVatRuleResultDto Failed(string error) => new()
    {
        Success = false,
        Errors = new[] { error }
    };

    public static DeleteVatRuleResultDto Failed(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// DTO for VAT rule conflict information.
/// </summary>
public record VatRuleConflictDto(
    Guid Id,
    string CountryCode,
    Guid? CategoryId,
    decimal TaxRate,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string ConflictDescription);
