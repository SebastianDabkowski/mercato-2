namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a VAT/tax rule that can be applied to transactions.
/// Rules can be defined per country and optionally per category.
/// Supports effective dates for handling rate changes over time.
/// </summary>
public class VatRule
{
    public Guid Id { get; private set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "DE", "FR", "PL").
    /// </summary>
    public string CountryCode { get; private set; } = default!;

    /// <summary>
    /// Optional category ID this rule applies to.
    /// Null means the rule applies to all categories in the country.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// VAT rate as a percentage (e.g., 23 for 23%).
    /// Stored with high precision for accurate calculations.
    /// </summary>
    public decimal TaxRate { get; private set; }

    /// <summary>
    /// Human-readable name for the VAT rate (e.g., "Standard Rate", "Reduced Rate").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Optional description or legal reference for this VAT rule.
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

    /// <summary>
    /// The ID of the user who created this rule.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// The ID of the user who last modified this rule.
    /// </summary>
    public Guid? LastModifiedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private VatRule()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new VAT rule.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="name">Human-readable name for the rate.</param>
    /// <param name="taxRate">VAT rate as a percentage.</param>
    /// <param name="createdByUserId">ID of the user creating the rule.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="effectiveFrom">Optional start date.</param>
    /// <param name="effectiveTo">Optional end date.</param>
    public VatRule(
        string countryCode,
        string name,
        decimal taxRate,
        Guid createdByUserId,
        Guid? categoryId = null,
        string? description = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        ValidateCountryCode(countryCode);
        ValidateName(name);
        ValidateTaxRate(taxRate);
        ValidateEffectiveDates(effectiveFrom, effectiveTo);

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user ID is required.", nameof(createdByUserId));
        }

        Id = Guid.NewGuid();
        CountryCode = countryCode.ToUpperInvariant();
        Name = name.Trim();
        TaxRate = taxRate;
        CreatedByUserId = createdByUserId;
        CategoryId = categoryId;
        Description = description?.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the VAT rate.
    /// </summary>
    public void UpdateTaxRate(decimal taxRate, Guid modifiedByUserId)
    {
        ValidateTaxRate(taxRate);
        ValidateModifier(modifiedByUserId);

        TaxRate = taxRate;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the name.
    /// </summary>
    public void UpdateName(string name, Guid modifiedByUserId)
    {
        ValidateName(name);
        ValidateModifier(modifiedByUserId);

        Name = name.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the description.
    /// </summary>
    public void UpdateDescription(string? description, Guid modifiedByUserId)
    {
        ValidateModifier(modifiedByUserId);

        Description = description?.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the effective date range.
    /// </summary>
    public void UpdateEffectiveDates(DateTime? effectiveFrom, DateTime? effectiveTo, Guid modifiedByUserId)
    {
        ValidateEffectiveDates(effectiveFrom, effectiveTo);
        ValidateModifier(modifiedByUserId);

        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the category association.
    /// </summary>
    public void UpdateCategory(Guid? categoryId, Guid modifiedByUserId)
    {
        ValidateModifier(modifiedByUserId);

        CategoryId = categoryId;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the VAT rule.
    /// </summary>
    public void Activate(Guid modifiedByUserId)
    {
        ValidateModifier(modifiedByUserId);

        IsActive = true;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the VAT rule.
    /// Deactivated rules are not applied to new transactions.
    /// </summary>
    public void Deactivate(Guid modifiedByUserId)
    {
        ValidateModifier(modifiedByUserId);

        IsActive = false;
        LastModifiedByUserId = modifiedByUserId;
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

    /// <summary>
    /// Checks if this rule has a future effective date (scheduled for future).
    /// </summary>
    public bool IsFutureDated => EffectiveFrom.HasValue && EffectiveFrom.Value > DateTime.UtcNow;

    /// <summary>
    /// Checks if this rule is currently effective (active and within date range).
    /// </summary>
    public bool IsCurrentlyEffective => IsEffectiveAt(DateTime.UtcNow);

    private static void ValidateCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Country code is required.", nameof(countryCode));
        }

        if (countryCode.Length != 2)
        {
            throw new ArgumentException("Country code must be a 2-letter ISO code.", nameof(countryCode));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateTaxRate(decimal taxRate)
    {
        if (taxRate < 0 || taxRate > 100)
        {
            throw new ArgumentException("Tax rate must be between 0 and 100.", nameof(taxRate));
        }
    }

    private static void ValidateEffectiveDates(DateTime? effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom.Value > effectiveTo.Value)
        {
            throw new ArgumentException("Effective from date must be before effective to date.");
        }
    }

    private static void ValidateModifier(Guid modifiedByUserId)
    {
        if (modifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Modifier user ID is required.", nameof(modifiedByUserId));
        }
    }
}
