using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserConsentRepository"/>.
/// </summary>
public sealed class UserConsentRepository : IUserConsentRepository
{
    private readonly AppDbContext _context;

    public UserConsentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserConsent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserConsents
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserConsent>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConsents
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserConsent?> GetActiveConsentAsync(
        Guid userId, Guid consentTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConsents
            .Where(c => c.UserId == userId && c.ConsentTypeId == consentTypeId)
            .OrderByDescending(c => c.ConsentedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserConsent?> GetActiveConsentByCodeAsync(
        Guid userId, string consentTypeCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = consentTypeCode.ToLowerInvariant();

        return await _context.UserConsents
            .Join(_context.ConsentTypes,
                consent => consent.ConsentTypeId,
                type => type.Id,
                (consent, type) => new { consent, type })
            .Where(x => x.consent.UserId == userId && x.type.Code == normalizedCode)
            .OrderByDescending(x => x.consent.ConsentedAt)
            .Select(x => x.consent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveConsentAsync(
        Guid userId, string consentTypeCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = consentTypeCode.ToLowerInvariant();

        return await _context.UserConsents
            .Join(_context.ConsentTypes,
                consent => consent.ConsentTypeId,
                type => type.Id,
                (consent, type) => new { consent, type })
            .Where(x => x.consent.UserId == userId && 
                        x.type.Code == normalizedCode && 
                        x.consent.IsGranted && 
                        x.consent.WithdrawnAt == null)
            .AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetUsersWithActiveConsentAsync(
        string consentTypeCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = consentTypeCode.ToLowerInvariant();

        return await _context.UserConsents
            .Join(_context.ConsentTypes,
                consent => consent.ConsentTypeId,
                type => type.Id,
                (consent, type) => new { consent, type })
            .Where(x => x.type.Code == normalizedCode && 
                        x.consent.IsGranted && 
                        x.consent.WithdrawnAt == null)
            .Select(x => x.consent.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default)
    {
        await _context.UserConsents.AddAsync(consent, cancellationToken);
    }

    public void Update(UserConsent consent)
    {
        _context.UserConsents.Update(consent);
    }

    public async Task AddAuditLogAsync(UserConsentAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.UserConsentAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserConsentAuditLog>> GetAuditLogsAsync(
        Guid userConsentId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConsentAuditLogs
            .AsNoTracking()
            .Where(l => l.UserConsentId == userConsentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserConsentAuditLog>> GetAuditLogsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConsentAuditLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConsentType?> GetConsentTypeByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentTypes
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<ConsentType?> GetConsentTypeByCodeAsync(
        string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _context.ConsentTypes
            .FirstOrDefaultAsync(t => t.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConsentType>> GetAllConsentTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ConsentTypes
            .AsNoTracking()
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConsentType>> GetActiveConsentTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ConsentTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddConsentTypeAsync(ConsentType consentType, CancellationToken cancellationToken = default)
    {
        await _context.ConsentTypes.AddAsync(consentType, cancellationToken);
    }

    public void UpdateConsentType(ConsentType consentType)
    {
        _context.ConsentTypes.Update(consentType);
    }

    public async Task<ConsentVersion?> GetConsentVersionByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentVersions
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<ConsentVersion?> GetCurrentVersionAsync(
        Guid consentTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentVersions
            .Where(v => v.ConsentTypeId == consentTypeId && v.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConsentVersion>> GetVersionsByConsentTypeAsync(
        Guid consentTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentVersions
            .AsNoTracking()
            .Where(v => v.ConsentTypeId == consentTypeId)
            .OrderByDescending(v => v.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task AddConsentVersionAsync(ConsentVersion version, CancellationToken cancellationToken = default)
    {
        await _context.ConsentVersions.AddAsync(version, cancellationToken);
    }

    public void UpdateConsentVersion(ConsentVersion version)
    {
        _context.ConsentVersions.Update(version);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
