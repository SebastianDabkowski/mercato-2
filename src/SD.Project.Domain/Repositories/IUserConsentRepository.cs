using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for user consent persistence operations.
/// </summary>
public interface IUserConsentRepository
{
    /// <summary>
    /// Gets a user consent by its ID.
    /// </summary>
    Task<UserConsent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consents for a specific user.
    /// </summary>
    Task<IReadOnlyCollection<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active consent for a user and consent type.
    /// </summary>
    Task<UserConsent?> GetActiveConsentAsync(Guid userId, Guid consentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active consent for a user by consent type code.
    /// </summary>
    Task<UserConsent?> GetActiveConsentByCodeAsync(Guid userId, string consentTypeCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has active consent for a specific consent type code.
    /// </summary>
    Task<bool> HasActiveConsentAsync(Guid userId, string consentTypeCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all user IDs that have active consent for a specific consent type code.
    /// Useful for bulk operations like marketing sends.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetUsersWithActiveConsentAsync(string consentTypeCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user consent.
    /// </summary>
    Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user consent.
    /// </summary>
    void Update(UserConsent consent);

    /// <summary>
    /// Adds an audit log entry.
    /// </summary>
    Task AddAuditLogAsync(UserConsentAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user consent.
    /// </summary>
    Task<IReadOnlyCollection<UserConsentAuditLog>> GetAuditLogsAsync(Guid userConsentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit logs for a user.
    /// </summary>
    Task<IReadOnlyCollection<UserConsentAuditLog>> GetAuditLogsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a consent type by ID.
    /// </summary>
    Task<ConsentType?> GetConsentTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a consent type by code.
    /// </summary>
    Task<ConsentType?> GetConsentTypeByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consent types.
    /// </summary>
    Task<IReadOnlyCollection<ConsentType>> GetAllConsentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active consent types.
    /// </summary>
    Task<IReadOnlyCollection<ConsentType>> GetActiveConsentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new consent type.
    /// </summary>
    Task AddConsentTypeAsync(ConsentType consentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing consent type.
    /// </summary>
    void UpdateConsentType(ConsentType consentType);

    /// <summary>
    /// Gets a consent version by ID.
    /// </summary>
    Task<ConsentVersion?> GetConsentVersionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version for a consent type.
    /// </summary>
    Task<ConsentVersion?> GetCurrentVersionAsync(Guid consentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions for a consent type.
    /// </summary>
    Task<IReadOnlyCollection<ConsentVersion>> GetVersionsByConsentTypeAsync(Guid consentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new consent version.
    /// </summary>
    Task AddConsentVersionAsync(ConsentVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing consent version.
    /// </summary>
    void UpdateConsentVersion(ConsentVersion version);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
