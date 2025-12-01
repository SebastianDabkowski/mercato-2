using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILegalDocumentRepository"/>.
/// </summary>
public sealed class LegalDocumentRepository : ILegalDocumentRepository
{
    private readonly AppDbContext _context;

    public LegalDocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LegalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<LegalDocument?> GetByTypeAsync(LegalDocumentType documentType, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .FirstOrDefaultAsync(d => d.DocumentType == documentType, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LegalDocument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .AsNoTracking()
            .OrderBy(d => d.DocumentType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LegalDocument>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.DocumentType)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LegalDocument document, CancellationToken cancellationToken = default)
    {
        await _context.LegalDocuments.AddAsync(document, cancellationToken);
    }

    public void Update(LegalDocument document)
    {
        _context.LegalDocuments.Update(document);
    }

    public async Task<LegalDocumentVersion?> GetVersionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LegalDocumentVersion>> GetVersionsByDocumentIdAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .AsNoTracking()
            .Where(v => v.LegalDocumentId == legalDocumentId)
            .OrderByDescending(v => v.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<LegalDocumentVersion?> GetCurrentVersionAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == legalDocumentId 
                && v.IsPublished 
                && v.EffectiveFrom <= now 
                && v.EffectiveTo == null)
            .OrderByDescending(v => v.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LegalDocumentVersion?> GetCurrentVersionByTypeAsync(
        LegalDocumentType documentType, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.LegalDocumentVersions
            .Join(_context.LegalDocuments,
                version => version.LegalDocumentId,
                document => document.Id,
                (version, document) => new { version, document })
            .Where(x => x.document.DocumentType == documentType 
                && x.document.IsActive
                && x.version.IsPublished 
                && x.version.EffectiveFrom <= now 
                && x.version.EffectiveTo == null)
            .OrderByDescending(x => x.version.EffectiveFrom)
            .Select(x => x.version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LegalDocumentVersion?> GetScheduledVersionAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == legalDocumentId 
                && v.IsPublished 
                && v.EffectiveFrom > now 
                && v.EffectiveTo == null)
            .OrderBy(v => v.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LegalDocumentVersion>> GetPublishedVersionsAsync(
        Guid legalDocumentId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .AsNoTracking()
            .Where(v => v.LegalDocumentId == legalDocumentId && v.IsPublished)
            .OrderByDescending(v => v.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task AddVersionAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default)
    {
        await _context.LegalDocumentVersions.AddAsync(version, cancellationToken);
    }

    public void UpdateVersion(LegalDocumentVersion version)
    {
        _context.LegalDocumentVersions.Update(version);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
