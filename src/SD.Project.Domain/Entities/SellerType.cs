namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the type of seller account.
/// </summary>
public enum SellerType
{
    /// <summary>
    /// Seller type has not been specified yet.
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    /// Seller operates as a registered company or business entity.
    /// </summary>
    Company = 1,

    /// <summary>
    /// Seller operates as an individual person.
    /// </summary>
    Individual = 2
}
