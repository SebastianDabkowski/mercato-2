using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing legal documents and their versions.
/// </summary>
public sealed class LegalDocumentService
{
    private readonly ILegalDocumentRepository _repository;

    public LegalDocumentService(ILegalDocumentRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all legal documents.
    /// </summary>
    public async Task<IReadOnlyCollection<LegalDocumentDto>> HandleAsync(
        GetAllLegalDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var documents = query.IncludeInactive
            ? await _repository.GetAllAsync(cancellationToken)
            : await _repository.GetActiveAsync(cancellationToken);

        var results = new List<LegalDocumentDto>();

        foreach (var document in documents)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(document.Id, cancellationToken);
            var scheduledVersion = await _repository.GetScheduledVersionAsync(document.Id, cancellationToken);
            results.Add(MapToDto(document, currentVersion, scheduledVersion));
        }

        return results.OrderBy(d => d.DocumentType).ToList();
    }

    /// <summary>
    /// Gets a legal document by ID.
    /// </summary>
    public async Task<LegalDocumentDto?> HandleAsync(
        GetLegalDocumentByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var document = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var currentVersion = await _repository.GetCurrentVersionAsync(document.Id, cancellationToken);
        var scheduledVersion = await _repository.GetScheduledVersionAsync(document.Id, cancellationToken);

        return MapToDto(document, currentVersion, scheduledVersion);
    }

    /// <summary>
    /// Gets a legal document by type.
    /// </summary>
    public async Task<LegalDocumentDto?> HandleAsync(
        GetLegalDocumentByTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var document = await _repository.GetByTypeAsync(query.DocumentType, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var currentVersion = await _repository.GetCurrentVersionAsync(document.Id, cancellationToken);
        var scheduledVersion = await _repository.GetScheduledVersionAsync(document.Id, cancellationToken);

        return MapToDto(document, currentVersion, scheduledVersion);
    }

    /// <summary>
    /// Gets the currently active legal document for public display.
    /// </summary>
    public async Task<LegalDocumentPublicDto?> HandleAsync(
        GetActiveLegalDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var document = await _repository.GetByTypeAsync(query.DocumentType, cancellationToken);
        if (document is null || !document.IsActive)
        {
            return null;
        }

        var currentVersion = await _repository.GetCurrentVersionByTypeAsync(query.DocumentType, cancellationToken);
        if (currentVersion is null)
        {
            return null;
        }

        var scheduledVersion = await _repository.GetScheduledVersionAsync(document.Id, cancellationToken);

        return new LegalDocumentPublicDto(
            document.DocumentType,
            document.Title,
            currentVersion.VersionNumber,
            currentVersion.Content,
            currentVersion.EffectiveFrom,
            scheduledVersion is not null,
            scheduledVersion?.EffectiveFrom,
            scheduledVersion?.ChangesSummary);
    }

    /// <summary>
    /// Gets all versions for a legal document.
    /// </summary>
    public async Task<IReadOnlyCollection<LegalDocumentVersionDto>> HandleAsync(
        GetLegalDocumentVersionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var versions = await _repository.GetVersionsByDocumentIdAsync(query.LegalDocumentId, cancellationToken);
        return versions.Select(MapToVersionDto).OrderByDescending(v => v.EffectiveFrom).ToList();
    }

    /// <summary>
    /// Gets a specific legal document version by ID.
    /// </summary>
    public async Task<LegalDocumentVersionDto?> HandleAsync(
        GetLegalDocumentVersionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var version = await _repository.GetVersionByIdAsync(query.VersionId, cancellationToken);
        return version is null ? null : MapToVersionDto(version);
    }

    /// <summary>
    /// Creates a new legal document.
    /// </summary>
    public async Task<LegalDocumentResultDto> HandleAsync(
        CreateLegalDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if document type already exists
        var existing = await _repository.GetByTypeAsync(command.DocumentType, cancellationToken);
        if (existing is not null)
        {
            return LegalDocumentResultDto.Failed($"A legal document of type '{GetDocumentTypeName(command.DocumentType)}' already exists.");
        }

        var document = new LegalDocument(
            command.DocumentType,
            command.Title,
            command.Description);

        await _repository.AddAsync(document, cancellationToken);

        LegalDocumentVersion? initialVersion = null;
        if (!string.IsNullOrWhiteSpace(command.InitialContent))
        {
            initialVersion = new LegalDocumentVersion(
                document.Id,
                command.InitialVersionNumber ?? "1.0",
                command.InitialContent,
                command.InitialEffectiveFrom ?? DateTime.UtcNow,
                null,
                command.CreatedBy);

            await _repository.AddVersionAsync(initialVersion, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return LegalDocumentResultDto.Succeeded(
            MapToDto(document, initialVersion, null),
            "Legal document created successfully.");
    }

    /// <summary>
    /// Updates a legal document's metadata.
    /// </summary>
    public async Task<LegalDocumentResultDto> HandleAsync(
        UpdateLegalDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (document is null)
        {
            return LegalDocumentResultDto.Failed("Legal document not found.");
        }

        document.Update(command.Title, command.Description);
        _repository.Update(document);
        await _repository.SaveChangesAsync(cancellationToken);

        var currentVersion = await _repository.GetCurrentVersionAsync(document.Id, cancellationToken);
        var scheduledVersion = await _repository.GetScheduledVersionAsync(document.Id, cancellationToken);

        return LegalDocumentResultDto.Succeeded(
            MapToDto(document, currentVersion, scheduledVersion),
            "Legal document updated successfully.");
    }

    /// <summary>
    /// Toggles a legal document's active status.
    /// </summary>
    public async Task<LegalDocumentResultDto> HandleAsync(
        ToggleLegalDocumentStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (document is null)
        {
            return LegalDocumentResultDto.Failed("Legal document not found.");
        }

        if (document.IsActive)
        {
            document.Deactivate();
        }
        else
        {
            document.Activate();
        }

        _repository.Update(document);
        await _repository.SaveChangesAsync(cancellationToken);

        var currentVersion = await _repository.GetCurrentVersionAsync(document.Id, cancellationToken);
        var scheduledVersion = await _repository.GetScheduledVersionAsync(document.Id, cancellationToken);

        var statusText = document.IsActive ? "activated" : "deactivated";
        return LegalDocumentResultDto.Succeeded(
            MapToDto(document, currentVersion, scheduledVersion),
            $"Legal document {statusText} successfully.");
    }

    /// <summary>
    /// Creates a new version of a legal document.
    /// </summary>
    public async Task<LegalDocumentVersionResultDto> HandleAsync(
        CreateLegalDocumentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await _repository.GetByIdAsync(command.LegalDocumentId, cancellationToken);
        if (document is null)
        {
            return LegalDocumentVersionResultDto.Failed("Legal document not found.");
        }

        // Check for duplicate version number
        var existingVersions = await _repository.GetVersionsByDocumentIdAsync(command.LegalDocumentId, cancellationToken);
        if (existingVersions.Any(v => v.VersionNumber.Equals(command.VersionNumber, StringComparison.OrdinalIgnoreCase)))
        {
            return LegalDocumentVersionResultDto.Failed($"Version '{command.VersionNumber}' already exists.");
        }

        var version = new LegalDocumentVersion(
            command.LegalDocumentId,
            command.VersionNumber,
            command.Content,
            command.EffectiveFrom,
            command.ChangesSummary,
            command.CreatedBy);

        await _repository.AddVersionAsync(version, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return LegalDocumentVersionResultDto.Succeeded(
            MapToVersionDto(version),
            "Version created successfully. Publish it when ready.");
    }

    /// <summary>
    /// Updates an unpublished legal document version.
    /// </summary>
    public async Task<LegalDocumentVersionResultDto> HandleAsync(
        UpdateLegalDocumentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var version = await _repository.GetVersionByIdAsync(command.VersionId, cancellationToken);
        if (version is null)
        {
            return LegalDocumentVersionResultDto.Failed("Version not found.");
        }

        if (version.IsPublished)
        {
            return LegalDocumentVersionResultDto.Failed("Published versions cannot be modified.");
        }

        version.Update(command.Content, command.EffectiveFrom, command.ChangesSummary);
        _repository.UpdateVersion(version);
        await _repository.SaveChangesAsync(cancellationToken);

        return LegalDocumentVersionResultDto.Succeeded(
            MapToVersionDto(version),
            "Version updated successfully.");
    }

    /// <summary>
    /// Publishes a legal document version.
    /// </summary>
    public async Task<LegalDocumentVersionResultDto> HandleAsync(
        PublishLegalDocumentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var version = await _repository.GetVersionByIdAsync(command.VersionId, cancellationToken);
        if (version is null)
        {
            return LegalDocumentVersionResultDto.Failed("Version not found.");
        }

        if (version.IsPublished)
        {
            return LegalDocumentVersionResultDto.Failed("Version is already published.");
        }

        // If this version is effective immediately or in the past, supersede the current version
        var now = DateTime.UtcNow;
        if (version.EffectiveFrom <= now)
        {
            var currentVersion = await _repository.GetCurrentVersionAsync(version.LegalDocumentId, cancellationToken);
            if (currentVersion is not null && currentVersion.Id != version.Id)
            {
                currentVersion.Supersede(version.EffectiveFrom);
                _repository.UpdateVersion(currentVersion);
            }
        }

        version.Publish();
        _repository.UpdateVersion(version);
        await _repository.SaveChangesAsync(cancellationToken);

        var message = version.EffectiveFrom > now
            ? $"Version published and scheduled to become effective on {version.EffectiveFrom:MMMM d, yyyy}."
            : "Version published and is now active.";

        return LegalDocumentVersionResultDto.Succeeded(
            MapToVersionDto(version),
            message);
    }

    private static LegalDocumentDto MapToDto(
        LegalDocument document,
        LegalDocumentVersion? currentVersion,
        LegalDocumentVersion? scheduledVersion)
    {
        return new LegalDocumentDto(
            document.Id,
            document.DocumentType,
            GetDocumentTypeName(document.DocumentType),
            document.Title,
            document.Description,
            document.IsActive,
            document.CreatedAt,
            document.UpdatedAt,
            currentVersion is null ? null : MapToVersionDto(currentVersion),
            scheduledVersion is null ? null : MapToVersionDto(scheduledVersion));
    }

    private static LegalDocumentVersionDto MapToVersionDto(LegalDocumentVersion version)
    {
        return new LegalDocumentVersionDto(
            version.Id,
            version.LegalDocumentId,
            version.VersionNumber,
            version.Content,
            version.ChangesSummary,
            version.EffectiveFrom,
            version.EffectiveTo,
            version.IsPublished,
            version.IsCurrentlyActive(),
            version.IsScheduled(),
            version.CreatedAt,
            version.UpdatedAt,
            version.CreatedBy);
    }

    private static string GetDocumentTypeName(LegalDocumentType documentType)
    {
        return documentType switch
        {
            LegalDocumentType.TermsOfService => "Terms of Service",
            LegalDocumentType.PrivacyPolicy => "Privacy Policy",
            LegalDocumentType.CookiePolicy => "Cookie Policy",
            LegalDocumentType.SellerAgreement => "Seller Agreement",
            _ => documentType.ToString()
        };
    }
}
