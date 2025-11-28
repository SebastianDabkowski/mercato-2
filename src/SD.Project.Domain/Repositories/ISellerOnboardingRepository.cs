using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for seller onboarding persistence operations.
/// </summary>
public interface ISellerOnboardingRepository
{
    /// <summary>
    /// Gets the onboarding record for the specified user.
    /// </summary>
    Task<SellerOnboarding?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the onboarding record by its ID.
    /// </summary>
    Task<SellerOnboarding?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new onboarding record.
    /// </summary>
    Task AddAsync(SellerOnboarding onboarding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists any changes to the onboarding records.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
