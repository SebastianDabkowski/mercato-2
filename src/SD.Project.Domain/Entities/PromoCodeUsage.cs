namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a record of a promo code being used in an order.
/// Used to track usage limits per user and globally.
/// </summary>
public class PromoCodeUsage
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The promo code that was used.
    /// </summary>
    public Guid PromoCodeId { get; private set; }

    /// <summary>
    /// The user who used the promo code.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The order in which the promo code was used.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The discount amount that was applied.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// When the promo code was used.
    /// </summary>
    public DateTime UsedAt { get; private set; }

    private PromoCodeUsage()
    {
        // EF Core constructor
    }

    public PromoCodeUsage(Guid promoCodeId, Guid userId, Guid orderId, decimal discountAmount)
    {
        if (promoCodeId == Guid.Empty)
        {
            throw new ArgumentException("Promo code ID is required.", nameof(promoCodeId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (discountAmount < 0)
        {
            throw new ArgumentException("Discount amount cannot be negative.", nameof(discountAmount));
        }

        Id = Guid.NewGuid();
        PromoCodeId = promoCodeId;
        UserId = userId;
        OrderId = orderId;
        DiscountAmount = discountAmount;
        UsedAt = DateTime.UtcNow;
    }
}
