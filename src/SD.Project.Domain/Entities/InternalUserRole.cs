namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the roles available to internal users within a store.
/// These roles determine the permissions for team members in the seller panel.
/// </summary>
public enum InternalUserRole
{
    /// <summary>
    /// Full access to all store operations and settings.
    /// Can manage team members and all aspects of the store.
    /// </summary>
    StoreOwner = 0,

    /// <summary>
    /// Can manage product catalog including create, update, and delete products.
    /// </summary>
    CatalogManager = 1,

    /// <summary>
    /// Can process orders, manage fulfillment, and handle customer inquiries.
    /// </summary>
    OrderManager = 2,

    /// <summary>
    /// Read-only access for viewing reports and store data.
    /// Typically used for accounting or auditing purposes.
    /// </summary>
    ReadOnly = 3
}
