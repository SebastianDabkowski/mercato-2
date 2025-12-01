using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for a VAT rule row in the list view.
/// </summary>
public sealed class VatRuleViewModel
{
    public Guid Id { get; init; }
    public string CountryCode { get; init; } = default!;
    public string CountryName { get; init; } = default!;
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal TaxRate { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string? CreatedByUserName { get; init; }
    public Guid? LastModifiedByUserId { get; init; }
    public string? LastModifiedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the formatted tax rate as percentage.
    /// </summary>
    public string FormattedRate => $"{TaxRate:N2}%";

    /// <summary>
    /// Gets the scope description (what the rule applies to).
    /// </summary>
    public string ScopeDisplay => CategoryId.HasValue
        ? $"{CountryName} - {CategoryName ?? "Unknown Category"}"
        : $"{CountryName} (All categories)";

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

    /// <summary>
    /// Gets the last modified info for display.
    /// </summary>
    public string LastModifiedDisplay => LastModifiedByUserName != null
        ? $"{LastModifiedByUserName} on {UpdatedAt:MMM d, yyyy 'at' h:mm tt}"
        : $"{CreatedByUserName ?? "Unknown"} on {CreatedAt:MMM d, yyyy 'at' h:mm tt}";
}

/// <summary>
/// View model for a VAT rule history entry.
/// </summary>
public sealed class VatRuleHistoryViewModel
{
    public Guid Id { get; init; }
    public Guid VatRuleId { get; init; }
    public VatRuleChangeType ChangeType { get; init; }
    public string CountryCode { get; init; } = default!;
    public string CountryName { get; init; } = default!;
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal TaxRate { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid ChangedByUserId { get; init; }
    public string ChangedByUserName { get; init; } = default!;
    public string? ChangeReason { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the formatted tax rate as percentage.
    /// </summary>
    public string FormattedRate => $"{TaxRate:N2}%";

    /// <summary>
    /// Gets a human-readable description of the change type.
    /// </summary>
    public string ChangeTypeDisplay => ChangeType switch
    {
        VatRuleChangeType.Created => "Created",
        VatRuleChangeType.Updated => "Updated",
        VatRuleChangeType.Activated => "Activated",
        VatRuleChangeType.Deactivated => "Deactivated",
        VatRuleChangeType.Deleted => "Deleted",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the badge CSS class for the change type.
    /// </summary>
    public string ChangeTypeBadgeClass => ChangeType switch
    {
        VatRuleChangeType.Created => "bg-success",
        VatRuleChangeType.Updated => "bg-primary",
        VatRuleChangeType.Activated => "bg-success",
        VatRuleChangeType.Deactivated => "bg-warning text-dark",
        VatRuleChangeType.Deleted => "bg-danger",
        _ => "bg-secondary"
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
}
