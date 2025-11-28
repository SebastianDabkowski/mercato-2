using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for PayoutSettings persistence operations.
/// </summary>
public interface IPayoutSettingsRepository
{
    /// <summary>
    /// Gets payout settings for a seller.
    /// </summary>
    Task<PayoutSettings?> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds new payout settings.
    /// </summary>
    Task AddAsync(PayoutSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
