using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for a commission rule row in the list view.
/// </summary>
public sealed class CommissionRuleViewModel
{
    public Guid Id { get; init; }
    public CommissionRuleType RuleType { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid? StoreId { get; init; }
    public string? StoreName { get; init; }
    public decimal CommissionRate { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets a display-friendly rule type name.
    /// </summary>
    public string RuleTypeDisplay => RuleType switch
    {
        CommissionRuleType.Global => "Global Default",
        CommissionRuleType.Category => "Category",
        CommissionRuleType.Seller => "Seller",
        _ => RuleType.ToString()
    };

    /// <summary>
    /// Gets the formatted commission rate as percentage.
    /// </summary>
    public string FormattedRate => $"{CommissionRate:N2}%";

    /// <summary>
    /// Gets the scope description (what the rule applies to).
    /// </summary>
    public string ScopeDisplay => RuleType switch
    {
        CommissionRuleType.Global => "All transactions",
        CommissionRuleType.Category => CategoryName ?? "Unknown Category",
        CommissionRuleType.Seller => StoreName ?? "Unknown Seller",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the formatted effective date range.
    /// </summary>
    public string EffectiveDateRangeDisplay => (EffectiveFrom, EffectiveTo) switch
    {
        (null, null) => "Always effective",
        (DateTime from, null) => $"From {from:MMM d, yyyy}",
        (null, DateTime to) => $"Until {to:MMM d, yyyy}",
        (DateTime from, DateTime to) => $"{from:MMM d, yyyy} - {to:MMM d, yyyy}"
    };

    /// <summary>
    /// Gets whether the rule is currently effective based on dates.
    /// </summary>
    public bool IsCurrentlyEffective
    {
        get
        {
            if (!IsActive) return false;
            var now = DateTime.UtcNow;
            if (EffectiveFrom.HasValue && now < EffectiveFrom.Value) return false;
            if (EffectiveTo.HasValue && now > EffectiveTo.Value) return false;
            return true;
        }
    }

    /// <summary>
    /// Gets whether this is a future-dated rule.
    /// </summary>
    public bool IsFutureDated => EffectiveFrom.HasValue && EffectiveFrom.Value > DateTime.UtcNow;

    /// <summary>
    /// Gets whether this rule has expired.
    /// </summary>
    public bool IsExpired => EffectiveTo.HasValue && EffectiveTo.Value < DateTime.UtcNow;

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass
    {
        get
        {
            if (!IsActive) return "bg-secondary";
            if (IsExpired) return "bg-warning text-dark";
            if (IsFutureDated) return "bg-info";
            return "bg-success";
        }
    }

    /// <summary>
    /// Gets the status display text.
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (!IsActive) return "Inactive";
            if (IsExpired) return "Expired";
            if (IsFutureDated) return "Scheduled";
            return "Active";
        }
    }
}
