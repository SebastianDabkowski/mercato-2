namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a personal data processing activity record as required by GDPR Article 30.
/// Used by compliance officers to maintain a registry of processing activities.
/// </summary>
public class DataProcessingActivity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The name/title of the processing activity.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// A description of the processing activity.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The purpose(s) of the processing (e.g., "Order fulfillment", "Marketing").
    /// </summary>
    public string Purpose { get; private set; } = default!;

    /// <summary>
    /// The legal basis for processing under GDPR Art. 6 (e.g., "Consent", "Contract", "Legal Obligation", "Legitimate Interest").
    /// </summary>
    public string LegalBasis { get; private set; } = default!;

    /// <summary>
    /// Categories of personal data processed (e.g., "Name, email, address", "Financial data").
    /// </summary>
    public string DataCategories { get; private set; } = default!;

    /// <summary>
    /// Categories of data subjects (e.g., "Customers", "Employees", "Website visitors").
    /// </summary>
    public string DataSubjects { get; private set; } = default!;

    /// <summary>
    /// Categories of recipients/processors who receive the data (e.g., "Payment providers", "Shipping partners").
    /// </summary>
    public string Processors { get; private set; } = string.Empty;

    /// <summary>
    /// Data retention period description (e.g., "5 years after contract end", "Until consent withdrawal").
    /// </summary>
    public string RetentionPeriod { get; private set; } = default!;

    /// <summary>
    /// Description of transfers to third countries (non-EU/EEA), if applicable.
    /// </summary>
    public string? InternationalTransfers { get; private set; }

    /// <summary>
    /// Description of technical and organizational security measures.
    /// </summary>
    public string? SecurityMeasures { get; private set; }

    /// <summary>
    /// Indicates whether the record is active or has been archived.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The ID of the user who created this record.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// The ID of the user who last modified this record.
    /// </summary>
    public Guid? LastModifiedByUserId { get; private set; }

    /// <summary>
    /// The UTC timestamp when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this record was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private DataProcessingActivity()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new data processing activity record.
    /// </summary>
    public DataProcessingActivity(
        string name,
        string purpose,
        string legalBasis,
        string dataCategories,
        string dataSubjects,
        string retentionPeriod,
        Guid createdByUserId,
        string? description = null,
        string? processors = null,
        string? internationalTransfers = null,
        string? securityMeasures = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Purpose is required.", nameof(purpose));
        }

        if (string.IsNullOrWhiteSpace(legalBasis))
        {
            throw new ArgumentException("Legal basis is required.", nameof(legalBasis));
        }

        if (string.IsNullOrWhiteSpace(dataCategories))
        {
            throw new ArgumentException("Data categories are required.", nameof(dataCategories));
        }

        if (string.IsNullOrWhiteSpace(dataSubjects))
        {
            throw new ArgumentException("Data subjects are required.", nameof(dataSubjects));
        }

        if (string.IsNullOrWhiteSpace(retentionPeriod))
        {
            throw new ArgumentException("Retention period is required.", nameof(retentionPeriod));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user ID is required.", nameof(createdByUserId));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Purpose = purpose.Trim();
        LegalBasis = legalBasis.Trim();
        DataCategories = dataCategories.Trim();
        DataSubjects = dataSubjects.Trim();
        Processors = processors?.Trim() ?? string.Empty;
        RetentionPeriod = retentionPeriod.Trim();
        InternationalTransfers = internationalTransfers?.Trim();
        SecurityMeasures = securityMeasures?.Trim();
        IsActive = true;
        CreatedByUserId = createdByUserId;
        LastModifiedByUserId = null;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the processing activity details.
    /// </summary>
    public void Update(
        string name,
        string purpose,
        string legalBasis,
        string dataCategories,
        string dataSubjects,
        string retentionPeriod,
        Guid modifiedByUserId,
        string? description = null,
        string? processors = null,
        string? internationalTransfers = null,
        string? securityMeasures = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Purpose is required.", nameof(purpose));
        }

        if (string.IsNullOrWhiteSpace(legalBasis))
        {
            throw new ArgumentException("Legal basis is required.", nameof(legalBasis));
        }

        if (string.IsNullOrWhiteSpace(dataCategories))
        {
            throw new ArgumentException("Data categories are required.", nameof(dataCategories));
        }

        if (string.IsNullOrWhiteSpace(dataSubjects))
        {
            throw new ArgumentException("Data subjects are required.", nameof(dataSubjects));
        }

        if (string.IsNullOrWhiteSpace(retentionPeriod))
        {
            throw new ArgumentException("Retention period is required.", nameof(retentionPeriod));
        }

        if (modifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Modifier user ID is required.", nameof(modifiedByUserId));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Purpose = purpose.Trim();
        LegalBasis = legalBasis.Trim();
        DataCategories = dataCategories.Trim();
        DataSubjects = dataSubjects.Trim();
        Processors = processors?.Trim() ?? string.Empty;
        RetentionPeriod = retentionPeriod.Trim();
        InternationalTransfers = internationalTransfers?.Trim();
        SecurityMeasures = securityMeasures?.Trim();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archives the processing activity record.
    /// </summary>
    public void Archive(Guid modifiedByUserId)
    {
        if (modifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Modifier user ID is required.", nameof(modifiedByUserId));
        }

        IsActive = false;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates an archived processing activity record.
    /// </summary>
    public void Reactivate(Guid modifiedByUserId)
    {
        if (modifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Modifier user ID is required.", nameof(modifiedByUserId));
        }

        IsActive = true;
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
