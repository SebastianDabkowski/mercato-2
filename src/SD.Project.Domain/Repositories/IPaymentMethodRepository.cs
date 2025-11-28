using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for payment method persistence operations.
/// </summary>
public interface IPaymentMethodRepository
{
    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active payment methods.
    /// </summary>
    Task<IReadOnlyList<PaymentMethod>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payment methods including inactive ones.
    /// </summary>
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default payment method.
    /// </summary>
    Task<PaymentMethod?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payment method.
    /// </summary>
    Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment method.
    /// </summary>
    Task UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
