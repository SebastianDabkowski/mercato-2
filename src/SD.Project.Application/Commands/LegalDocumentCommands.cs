using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new legal document.
/// </summary>
public record CreateLegalDocumentCommand(
    LegalDocumentType DocumentType,
    string Title,
    string? Description = null,
    string? InitialContent = null,
    string? InitialVersionNumber = null,
    DateTime? InitialEffectiveFrom = null,
    Guid? CreatedBy = null);

/// <summary>
/// Command to update a legal document's metadata.
/// </summary>
public record UpdateLegalDocumentCommand(
    Guid Id,
    string Title,
    string? Description);

/// <summary>
/// Command to toggle a legal document's active status.
/// </summary>
public record ToggleLegalDocumentStatusCommand(Guid Id);

/// <summary>
/// Command to create a new version of a legal document.
/// </summary>
public record CreateLegalDocumentVersionCommand(
    Guid LegalDocumentId,
    string VersionNumber,
    string Content,
    DateTime EffectiveFrom,
    string? ChangesSummary = null,
    Guid? CreatedBy = null);

/// <summary>
/// Command to update an unpublished legal document version.
/// </summary>
public record UpdateLegalDocumentVersionCommand(
    Guid VersionId,
    string Content,
    DateTime EffectiveFrom,
    string? ChangesSummary);

/// <summary>
/// Command to publish a legal document version.
/// </summary>
public record PublishLegalDocumentVersionCommand(Guid VersionId);
