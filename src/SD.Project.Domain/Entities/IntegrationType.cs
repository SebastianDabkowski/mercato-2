namespace SD.Project.Domain.Entities;

/// <summary>
/// Types of external integrations supported by the marketplace.
/// </summary>
public enum IntegrationType
{
    /// <summary>Payment gateway integration (e.g., Stripe, PayPal).</summary>
    Payment,

    /// <summary>Shipping/logistics provider integration.</summary>
    Shipping,

    /// <summary>Enterprise Resource Planning system integration.</summary>
    Erp,

    /// <summary>E-commerce platform connector.</summary>
    Ecommerce,

    /// <summary>Other type of integration.</summary>
    Other
}
