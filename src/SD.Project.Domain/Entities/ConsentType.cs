namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a type of consent that users can grant or withdraw.
/// Examples: Newsletter, Profiling, Third-party sharing, etc.
/// </summary>
public class ConsentType
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique code for the consent type (e.g., "newsletter", "profiling", "third_party_sharing").
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>
    /// Display name for the consent type.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Detailed description of what granting this consent means.
    /// </summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Indicates whether this consent type is currently active and should be presented to users.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Indicates whether this consent can be pre-selected by default (only allowed where legally permitted).
    /// </summary>
    public bool AllowPreselection { get; private set; }

    /// <summary>
    /// Indicates whether this consent is required (cannot be declined).
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Display order for sorting consent types in the UI.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// The UTC timestamp when this consent type was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this consent type was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private ConsentType()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new consent type.
    /// </summary>
    public ConsentType(
        string code,
        string name,
        string description,
        bool allowPreselection = false,
        bool isRequired = false,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        Id = Guid.NewGuid();
        Code = code.Trim().ToLowerInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = true;
        AllowPreselection = allowPreselection;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the consent type details.
    /// </summary>
    public void Update(
        string name,
        string description,
        bool allowPreselection,
        bool isRequired,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        Name = name.Trim();
        Description = description.Trim();
        AllowPreselection = allowPreselection;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the consent type.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the consent type.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
