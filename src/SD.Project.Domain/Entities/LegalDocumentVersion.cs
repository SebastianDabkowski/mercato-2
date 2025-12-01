namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a specific version of a legal document.
/// Each version has an effective date and can be scheduled for future activation.
/// </summary>
public class LegalDocumentVersion
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the legal document this version belongs to.
    /// </summary>
    public Guid LegalDocumentId { get; private set; }

    /// <summary>
    /// Version identifier (e.g., "1.0", "2.0", "2024-01").
    /// </summary>
    public string VersionNumber { get; private set; } = default!;

    /// <summary>
    /// The full legal content that users must agree to (supports HTML).
    /// </summary>
    public string Content { get; private set; } = default!;

    /// <summary>
    /// Optional summary of changes in this version.
    /// </summary>
    public string? ChangesSummary { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version becomes effective.
    /// If in the future, this version is scheduled but not yet active.
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version was superseded (null if still active or scheduled).
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Indicates whether this version is published and visible.
    /// </summary>
    public bool IsPublished { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this version was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// The user ID of who created this version.
    /// </summary>
    public Guid? CreatedBy { get; private set; }

    private LegalDocumentVersion()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new legal document version.
    /// </summary>
    public LegalDocumentVersion(
        Guid legalDocumentId,
        string versionNumber,
        string content,
        DateTime effectiveFrom,
        string? changesSummary = null,
        Guid? createdBy = null)
    {
        if (legalDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Legal document ID is required.", nameof(legalDocumentId));
        }

        if (string.IsNullOrWhiteSpace(versionNumber))
        {
            throw new ArgumentException("Version number is required.", nameof(versionNumber));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }

        Id = Guid.NewGuid();
        LegalDocumentId = legalDocumentId;
        VersionNumber = versionNumber.Trim();
        Content = content.Trim();
        ChangesSummary = changesSummary?.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = null;
        IsPublished = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Updates the version content and metadata.
    /// Can only be updated before publishing.
    /// </summary>
    public void Update(string content, DateTime effectiveFrom, string? changesSummary)
    {
        if (IsPublished)
        {
            throw new InvalidOperationException("Published versions cannot be modified.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }

        Content = content.Trim();
        EffectiveFrom = effectiveFrom;
        ChangesSummary = changesSummary?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Publishes the version, making it available for users.
    /// </summary>
    public void Publish()
    {
        if (IsPublished)
        {
            throw new InvalidOperationException("Version is already published.");
        }

        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Supersedes this version with a new version.
    /// </summary>
    public void Supersede(DateTime supersededAt)
    {
        if (EffectiveTo.HasValue)
        {
            throw new InvalidOperationException("Version has already been superseded.");
        }

        EffectiveTo = supersededAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this version is currently active (effective and not superseded).
    /// </summary>
    public bool IsCurrentlyActive(DateTime? referenceTime = null)
    {
        var now = referenceTime ?? DateTime.UtcNow;
        return IsPublished && EffectiveFrom <= now && !EffectiveTo.HasValue;
    }

    /// <summary>
    /// Checks if this version is scheduled for future activation.
    /// </summary>
    public bool IsScheduled(DateTime? referenceTime = null)
    {
        var now = referenceTime ?? DateTime.UtcNow;
        return IsPublished && EffectiveFrom > now && !EffectiveTo.HasValue;
    }
}
