using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all legal documents.
/// </summary>
public record GetAllLegalDocumentsQuery(bool IncludeInactive = false);

/// <summary>
/// Query to get a legal document by ID.
/// </summary>
public record GetLegalDocumentByIdQuery(Guid Id);

/// <summary>
/// Query to get a legal document by type.
/// </summary>
public record GetLegalDocumentByTypeQuery(LegalDocumentType DocumentType);

/// <summary>
/// Query to get all versions for a legal document.
/// </summary>
public record GetLegalDocumentVersionsQuery(Guid LegalDocumentId);

/// <summary>
/// Query to get a specific legal document version by ID.
/// </summary>
public record GetLegalDocumentVersionByIdQuery(Guid VersionId);

/// <summary>
/// Query to get the currently active version for a legal document type (for public display).
/// </summary>
public record GetActiveLegalDocumentQuery(LegalDocumentType DocumentType);
