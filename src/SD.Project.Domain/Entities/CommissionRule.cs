namespace SD.Project.Domain.Entities;

/// <summary>
/// Specifies the type of commission rule.
/// </summary>
public enum CommissionRuleType
{
    /// <summary>Rule applies globally as the platform default.</summary>
    Global,
    /// <summary>Rule applies to a specific category.</summary>
    Category,
    /// <summary>Rule applies to a specific seller/store.</summary>
    Seller
}

/// <summary>
/// Represents a commission rule for the platform.
/// Rules can be global, category-specific, or seller-specific.
/// More specific rules (seller > category > global) take precedence.
/// </summary>
public class CommissionRule
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The type of commission rule.
    /// </summary>
    public CommissionRuleType RuleType { get; private set; }

    /// <summary>
    /// The category ID this rule applies to (when RuleType is Category).
    /// Null for Global or Seller rules.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// The store ID this rule applies to (when RuleType is Seller).
    /// Null for Global or Category rules.
    /// </summary>
    public Guid? StoreId { get; private set; }

    /// <summary>
    /// Commission rate as a percentage (e.g., 10 for 10%).
    /// Stored with high precision for accurate calculations.
    /// </summary>
    public decimal CommissionRate { get; private set; }

    /// <summary>
    /// Optional description or reason for this commission rule.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this rule is currently active.
    /// Inactive rules are not applied to new transactions.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The date from which this rule is effective.
    /// Null means effective immediately.
    /// </summary>
    public DateTime? EffectiveFrom { get; private set; }

    /// <summary>
    /// The date until which this rule is effective.
    /// Null means no end date.
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CommissionRule()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new global commission rule.
    /// </summary>
    public static CommissionRule CreateGlobalRule(
        decimal commissionRate,
        string? description = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        ValidateCommissionRate(commissionRate);
        ValidateEffectiveDates(effectiveFrom, effectiveTo);

        return new CommissionRule
        {
            Id = Guid.NewGuid(),
            RuleType = CommissionRuleType.Global,
            CategoryId = null,
            StoreId = null,
            CommissionRate = commissionRate,
            Description = description?.Trim(),
            IsActive = true,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new category-specific commission rule.
    /// </summary>
    public static CommissionRule CreateCategoryRule(
        Guid categoryId,
        decimal commissionRate,
        string? description = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID is required.", nameof(categoryId));
        }

        ValidateCommissionRate(commissionRate);
        ValidateEffectiveDates(effectiveFrom, effectiveTo);

        return new CommissionRule
        {
            Id = Guid.NewGuid(),
            RuleType = CommissionRuleType.Category,
            CategoryId = categoryId,
            StoreId = null,
            CommissionRate = commissionRate,
            Description = description?.Trim(),
            IsActive = true,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new seller-specific commission rule.
    /// </summary>
    public static CommissionRule CreateSellerRule(
        Guid storeId,
        decimal commissionRate,
        string? description = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        ValidateCommissionRate(commissionRate);
        ValidateEffectiveDates(effectiveFrom, effectiveTo);

        return new CommissionRule
        {
            Id = Guid.NewGuid(),
            RuleType = CommissionRuleType.Seller,
            CategoryId = null,
            StoreId = storeId,
            CommissionRate = commissionRate,
            Description = description?.Trim(),
            IsActive = true,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the commission rate.
    /// </summary>
    public void UpdateCommissionRate(decimal commissionRate)
    {
        ValidateCommissionRate(commissionRate);
        CommissionRate = commissionRate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the effective date range.
    /// </summary>
    public void UpdateEffectiveDates(DateTime? effectiveFrom, DateTime? effectiveTo)
    {
        ValidateEffectiveDates(effectiveFrom, effectiveTo);
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the commission rule.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the commission rule.
    /// Deactivated rules are not applied to new transactions.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the rule is currently effective based on date range.
    /// </summary>
    public bool IsEffectiveAt(DateTime dateTime)
    {
        if (!IsActive)
        {
            return false;
        }

        if (EffectiveFrom.HasValue && dateTime < EffectiveFrom.Value)
        {
            return false;
        }

        if (EffectiveTo.HasValue && dateTime > EffectiveTo.Value)
        {
            return false;
        }

        return true;
    }

    private static void ValidateCommissionRate(decimal commissionRate)
    {
        if (commissionRate < 0 || commissionRate > 100)
        {
            throw new ArgumentException("Commission rate must be between 0 and 100.", nameof(commissionRate));
        }
    }

    private static void ValidateEffectiveDates(DateTime? effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom.Value > effectiveTo.Value)
        {
            throw new ArgumentException("Effective from date must be before effective to date.");
        }
    }
}
