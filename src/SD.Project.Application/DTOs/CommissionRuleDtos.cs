using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a commission rule for the UI layer.
/// </summary>
public sealed record CommissionRuleDto(
    Guid Id,
    CommissionRuleType RuleType,
    Guid? CategoryId,
    string? CategoryName,
    Guid? StoreId,
    string? StoreName,
    decimal CommissionRate,
    string? Description,
    bool IsActive,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO representing the result of a commission rule creation or update operation.
/// </summary>
public sealed record CommissionRuleResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public CommissionRuleDto? CommissionRule { get; init; }

    public static CommissionRuleResultDto Succeeded(CommissionRuleDto rule, string message = "Commission rule saved successfully.")
    {
        return new CommissionRuleResultDto
        {
            Success = true,
            Message = message,
            CommissionRule = rule
        };
    }

    public static CommissionRuleResultDto Failed(string error)
    {
        return new CommissionRuleResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static CommissionRuleResultDto Failed(IReadOnlyList<string> errors)
    {
        return new CommissionRuleResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}

/// <summary>
/// DTO representing the result of a commission rule deletion operation.
/// </summary>
public sealed record DeleteCommissionRuleResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static DeleteCommissionRuleResultDto Succeeded(string message = "Commission rule deleted successfully.")
    {
        return new DeleteCommissionRuleResultDto
        {
            Success = true,
            Message = message
        };
    }

    public static DeleteCommissionRuleResultDto Failed(string error)
    {
        return new DeleteCommissionRuleResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static DeleteCommissionRuleResultDto Failed(IReadOnlyList<string> errors)
    {
        return new DeleteCommissionRuleResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}

/// <summary>
/// Result DTO for conflict validation.
/// </summary>
public sealed record CommissionRuleConflictDto(
    Guid ConflictingRuleId,
    CommissionRuleType RuleType,
    decimal CommissionRate,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string Description);
