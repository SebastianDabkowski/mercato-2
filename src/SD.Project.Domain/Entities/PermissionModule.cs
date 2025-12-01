namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the modules in the marketplace platform for RBAC grouping.
/// </summary>
public enum PermissionModule
{
    /// <summary>Product catalog management module.</summary>
    Product = 1,

    /// <summary>Order management module.</summary>
    Order = 2,

    /// <summary>Returns and disputes module.</summary>
    Return = 3,

    /// <summary>User management module.</summary>
    User = 4,

    /// <summary>Store management module.</summary>
    Store = 5,

    /// <summary>Payments and settlements module.</summary>
    Payment = 6,

    /// <summary>Reviews and ratings module.</summary>
    Review = 7,

    /// <summary>Reporting and analytics module.</summary>
    Report = 8,

    /// <summary>Category management module.</summary>
    Category = 9,

    /// <summary>Platform settings module.</summary>
    Settings = 10,

    /// <summary>Compliance and regulatory module.</summary>
    Compliance = 11,

    /// <summary>Role-based access control module.</summary>
    Rbac = 12
}
