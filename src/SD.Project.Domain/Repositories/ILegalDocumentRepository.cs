using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for legal document persistence operations.
/// </summary>
public interface ILegalDocumentRepository
{
    /// <summary>
    /// Gets a legal document by its ID.
    /// </summary>
    Task<LegalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a legal document by its type.
    /// </summary>
    Task<LegalDocument?> GetByTypeAsync(LegalDocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all legal documents.
    /// </summary>
    Task<IReadOnlyCollection<LegalDocument>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active legal documents.
    /// </summary>
    Task<IReadOnlyCollection<LegalDocument>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new legal document.
    /// </summary>
    Task AddAsync(LegalDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing legal document.
    /// </summary>
    void Update(LegalDocument document);

    /// <summary>
    /// Gets a legal document version by its ID.
    /// </summary>
    Task<LegalDocumentVersion?> GetVersionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions for a legal document.
    /// </summary>
    Task<IReadOnlyCollection<LegalDocumentVersion>> GetVersionsByDocumentIdAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active version for a legal document (effective and not superseded).
    /// </summary>
    Task<LegalDocumentVersion?> GetCurrentVersionAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active version for a legal document type.
    /// </summary>
    Task<LegalDocumentVersion?> GetCurrentVersionByTypeAsync(
        LegalDocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next scheduled version for a legal document (future effective date).
    /// </summary>
    Task<LegalDocumentVersion?> GetScheduledVersionAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all published versions for a legal document ordered by effective date.
    /// </summary>
    Task<IReadOnlyCollection<LegalDocumentVersion>> GetPublishedVersionsAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new legal document version.
    /// </summary>
    Task AddVersionAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing legal document version.
    /// </summary>
    void UpdateVersion(LegalDocumentVersion version);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
