namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the roles available to users in the marketplace.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// A buyer who can purchase products from sellers.
    /// </summary>
    Buyer = 0,

    /// <summary>
    /// A seller who can list and sell products.
    /// </summary>
    Seller = 1,

    /// <summary>
    /// An administrator with full platform access.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// A support agent who can assist with customer issues.
    /// </summary>
    Support = 3,

    /// <summary>
    /// A compliance officer who can review and approve regulatory matters.
    /// </summary>
    Compliance = 4
}
