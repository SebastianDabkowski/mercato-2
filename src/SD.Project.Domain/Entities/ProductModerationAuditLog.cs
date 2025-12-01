namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a moderation decision record for audit and dispute resolution.
/// </summary>
public class ProductModerationAuditLog
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ModeratorId { get; private set; }
    public ProductModerationStatus Decision { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductModerationAuditLog()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new moderation audit log entry.
    /// </summary>
    /// <param name="productId">The ID of the moderated product.</param>
    /// <param name="moderatorId">The ID of the moderator.</param>
    /// <param name="decision">The moderation decision.</param>
    /// <param name="reason">Optional reason for the decision.</param>
    public ProductModerationAuditLog(Guid productId, Guid moderatorId, ProductModerationStatus decision, string? reason = null)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (moderatorId == Guid.Empty)
        {
            throw new ArgumentException("Moderator ID is required.", nameof(moderatorId));
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        ModeratorId = moderatorId;
        Decision = decision;
        Reason = reason?.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
