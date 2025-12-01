namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a historical record of a VAT rule change.
/// Used for audit trail and compliance tracking.
/// </summary>
public class VatRuleHistory
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the VAT rule this history entry relates to.
    /// </summary>
    public Guid VatRuleId { get; private set; }

    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public VatRuleChangeType ChangeType { get; private set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code at the time of change.
    /// </summary>
    public string CountryCode { get; private set; } = default!;

    /// <summary>
    /// Category ID at the time of change.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// VAT rate at the time of change.
    /// </summary>
    public decimal TaxRate { get; private set; }

    /// <summary>
    /// Name at the time of change.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Description at the time of change.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// IsActive status at the time of change.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Effective from date at the time of change.
    /// </summary>
    public DateTime? EffectiveFrom { get; private set; }

    /// <summary>
    /// Effective to date at the time of change.
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// The ID of the user who made the change.
    /// </summary>
    public Guid ChangedByUserId { get; private set; }

    /// <summary>
    /// The name of the user who made the change (denormalized for audit purposes).
    /// </summary>
    public string ChangedByUserName { get; private set; } = default!;

    /// <summary>
    /// Optional reason or notes for the change.
    /// </summary>
    public string? ChangeReason { get; private set; }

    /// <summary>
    /// Timestamp when this history entry was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private VatRuleHistory()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a history entry from a VAT rule.
    /// </summary>
    public static VatRuleHistory FromVatRule(
        VatRule vatRule,
        VatRuleChangeType changeType,
        Guid changedByUserId,
        string changedByUserName,
        string? changeReason = null)
    {
        ArgumentNullException.ThrowIfNull(vatRule);

        if (changedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Changed by user ID is required.", nameof(changedByUserId));
        }

        if (string.IsNullOrWhiteSpace(changedByUserName))
        {
            throw new ArgumentException("Changed by user name is required.", nameof(changedByUserName));
        }

        return new VatRuleHistory
        {
            Id = Guid.NewGuid(),
            VatRuleId = vatRule.Id,
            ChangeType = changeType,
            CountryCode = vatRule.CountryCode,
            CategoryId = vatRule.CategoryId,
            TaxRate = vatRule.TaxRate,
            Name = vatRule.Name,
            Description = vatRule.Description,
            IsActive = vatRule.IsActive,
            EffectiveFrom = vatRule.EffectiveFrom,
            EffectiveTo = vatRule.EffectiveTo,
            ChangedByUserId = changedByUserId,
            ChangedByUserName = changedByUserName.Trim(),
            ChangeReason = changeReason?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Specifies the type of change made to a VAT rule.
/// </summary>
public enum VatRuleChangeType
{
    /// <summary>Rule was created.</summary>
    Created,

    /// <summary>Rule was updated.</summary>
    Updated,

    /// <summary>Rule was activated.</summary>
    Activated,

    /// <summary>Rule was deactivated.</summary>
    Deactivated,

    /// <summary>Rule was deleted.</summary>
    Deleted
}
