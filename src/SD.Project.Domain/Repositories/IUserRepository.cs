using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for user persistence operations.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalLoginAsync(ExternalLoginProvider provider, string externalId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of users filtered by optional criteria.
    /// </summary>
    /// <param name="roleFilter">Optional role filter.</param>
    /// <param name="statusFilter">Optional status filter.</param>
    /// <param name="searchTerm">Optional search term to filter by email, first name, last name, or ID.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the list of users and the total count.</returns>
    Task<(IReadOnlyList<User> Users, int TotalCount)> GetFilteredUsersAsync(
        UserRole? roleFilter,
        UserStatus? statusFilter,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
