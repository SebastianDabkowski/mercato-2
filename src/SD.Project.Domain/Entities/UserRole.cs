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
    Seller = 1
}
