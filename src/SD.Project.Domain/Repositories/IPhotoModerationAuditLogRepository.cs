using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for photo moderation audit log persistence operations.
/// </summary>
public interface IPhotoModerationAuditLogRepository
{
    Task<PhotoModerationAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PhotoModerationAuditLog>> GetByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default);
    Task AddAsync(PhotoModerationAuditLog auditLog, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
