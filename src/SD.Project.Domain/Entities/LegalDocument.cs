namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a legal document such as Terms of Service or Privacy Policy.
/// This is the parent entity that holds metadata about the document type.
/// </summary>
public class LegalDocument
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The type of legal document.
    /// </summary>
    public LegalDocumentType DocumentType { get; private set; }

    /// <summary>
    /// Display name for the legal document.
    /// </summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// Brief description of what this document covers.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Indicates whether this legal document is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The UTC timestamp when this document was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this document was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private LegalDocument()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new legal document.
    /// </summary>
    public LegalDocument(
        LegalDocumentType documentType,
        string title,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Id = Guid.NewGuid();
        DocumentType = documentType;
        Title = title.Trim();
        Description = description?.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the document metadata.
    /// </summary>
    public void Update(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the legal document.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the legal document.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
