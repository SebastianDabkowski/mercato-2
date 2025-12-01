namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a specific version of a consent text.
/// Consent texts must be versioned and linked to each user decision.
/// </summary>
public class ConsentVersion
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the consent type this version belongs to.
    /// </summary>
    public Guid ConsentTypeId { get; private set; }

    /// <summary>
    /// Version identifier (e.g., "1.0", "2.0", "2024-01").
    /// </summary>
    public string Version { get; private set; } = default!;

    /// <summary>
    /// The full consent text that the user agrees to.
    /// </summary>
    public string ConsentText { get; private set; } = default!;

    /// <summary>
    /// Indicates whether this is the currently active version for the consent type.
    /// </summary>
    public bool IsCurrent { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version became effective.
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version was superseded (null if still current).
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private ConsentVersion()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new consent version.
    /// </summary>
    public ConsentVersion(
        Guid consentTypeId,
        string version,
        string consentText,
        DateTime effectiveFrom)
    {
        if (consentTypeId == Guid.Empty)
        {
            throw new ArgumentException("Consent type ID is required.", nameof(consentTypeId));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version is required.", nameof(version));
        }

        if (string.IsNullOrWhiteSpace(consentText))
        {
            throw new ArgumentException("Consent text is required.", nameof(consentText));
        }

        Id = Guid.NewGuid();
        ConsentTypeId = consentTypeId;
        Version = version.Trim();
        ConsentText = consentText.Trim();
        IsCurrent = true;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = null;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Supersedes this version with a new version.
    /// </summary>
    public void Supersede(DateTime supersededAt)
    {
        if (!IsCurrent)
        {
            throw new InvalidOperationException("Only current versions can be superseded.");
        }

        IsCurrent = false;
        EffectiveTo = supersededAt;
    }
}
