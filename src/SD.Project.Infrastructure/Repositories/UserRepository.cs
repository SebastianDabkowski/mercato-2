using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed user repository.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<User>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(x => idList.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<User?> GetByExternalLoginAsync(ExternalLoginProvider provider, string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(
            x => x.ExternalProvider == provider && x.ExternalId == externalId,
            cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetFilteredUsersAsync(
        UserRole? roleFilter,
        UserStatus? statusFilter,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        // Apply role filter
        if (roleFilter.HasValue)
        {
            query = query.Where(u => u.Role == roleFilter.Value);
        }

        // Apply status filter
        if (statusFilter.HasValue)
        {
            query = query.Where(u => u.Status == statusFilter.Value);
        }

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
            
            // Try to parse as GUID for ID search
            if (Guid.TryParse(searchTerm.Trim(), out var searchGuid))
            {
                query = query.Where(u => u.Id == searchGuid);
            }
            else
            {
                // Search by email, first name, or last name
                query = query.Where(u =>
                    EF.Functions.Like(u.FirstName.ToLower(), $"%{normalizedSearchTerm}%") ||
                    EF.Functions.Like(u.LastName.ToLower(), $"%{normalizedSearchTerm}%") ||
                    EF.Functions.Like(u.Email.Value.ToLower(), $"%{normalizedSearchTerm}%"));
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }
}
