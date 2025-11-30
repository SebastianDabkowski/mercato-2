using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for managing promo codes.
/// </summary>
public interface IPromoCodeRepository
{
    /// <summary>
    /// Gets a promo code by its unique code string.
    /// </summary>
    /// <param name="code">The promo code string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The promo code if found, null otherwise.</returns>
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a promo code by its ID.
    /// </summary>
    /// <param name="id">The promo code ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The promo code if found, null otherwise.</returns>
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of times a user has used a specific promo code.
    /// </summary>
    /// <param name="promoCodeId">The promo code ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage count for this user.</returns>
    Task<int> GetUserUsageCountAsync(Guid promoCodeId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a usage of a promo code by a user.
    /// </summary>
    /// <param name="promoCodeId">The promo code ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="orderId">The order ID.</param>
    /// <param name="discountAmount">The discount amount applied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordUsageAsync(Guid promoCodeId, Guid userId, Guid orderId, decimal discountAmount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing promo code.
    /// </summary>
    /// <param name="promoCode">The promo code to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new promo code.
    /// </summary>
    /// <param name="promoCode">The promo code to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
