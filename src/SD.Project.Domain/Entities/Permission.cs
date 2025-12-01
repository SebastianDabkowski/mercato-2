namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the permissions available in the marketplace platform.
/// Permissions are grouped by module for easier management.
/// </summary>
public enum Permission
{
    // Product Module Permissions (100-199)
    /// <summary>View product listings.</summary>
    ProductView = 100,
    /// <summary>Create new products.</summary>
    ProductCreate = 101,
    /// <summary>Edit existing products.</summary>
    ProductEdit = 102,
    /// <summary>Delete products.</summary>
    ProductDelete = 103,
    /// <summary>Import products in bulk.</summary>
    ProductImport = 104,
    /// <summary>Export product data.</summary>
    ProductExport = 105,
    /// <summary>Moderate products (approve/reject).</summary>
    ProductModerate = 106,

    // Order Module Permissions (200-299)
    /// <summary>View orders.</summary>
    OrderView = 200,
    /// <summary>Process orders (update status, fulfill).</summary>
    OrderProcess = 201,
    /// <summary>Cancel orders.</summary>
    OrderCancel = 202,
    /// <summary>Export order data.</summary>
    OrderExport = 203,
    /// <summary>View all orders (admin/support).</summary>
    OrderViewAll = 204,

    // Return/Dispute Module Permissions (300-399)
    /// <summary>View returns and disputes.</summary>
    ReturnView = 300,
    /// <summary>Process returns (approve/reject).</summary>
    ReturnProcess = 301,
    /// <summary>Escalate disputes to admin.</summary>
    ReturnEscalate = 302,
    /// <summary>Resolve disputes as admin.</summary>
    ReturnResolve = 303,

    // User Management Module Permissions (400-499)
    /// <summary>View user list.</summary>
    UserView = 400,
    /// <summary>Create new users.</summary>
    UserCreate = 401,
    /// <summary>Edit user details.</summary>
    UserEdit = 402,
    /// <summary>Block/unblock users.</summary>
    UserBlock = 403,
    /// <summary>Assign roles to users.</summary>
    UserAssignRole = 404,
    /// <summary>View user analytics.</summary>
    UserAnalytics = 405,

    // Store Management Module Permissions (500-599)
    /// <summary>View store details.</summary>
    StoreView = 500,
    /// <summary>Edit store settings.</summary>
    StoreEdit = 501,
    /// <summary>Manage store team members.</summary>
    StoreManageTeam = 502,
    /// <summary>Approve/reject seller onboarding.</summary>
    StoreApproveOnboarding = 503,

    // Payment/Settlement Module Permissions (600-699)
    /// <summary>View payment information.</summary>
    PaymentView = 600,
    /// <summary>Process refunds.</summary>
    PaymentRefund = 601,
    /// <summary>View settlements.</summary>
    SettlementView = 602,
    /// <summary>Process payouts.</summary>
    PayoutProcess = 603,
    /// <summary>Manage commission rules.</summary>
    CommissionManage = 604,
    /// <summary>View commission reports.</summary>
    CommissionView = 605,

    // Review/Rating Module Permissions (700-799)
    /// <summary>View reviews.</summary>
    ReviewView = 700,
    /// <summary>Create reviews.</summary>
    ReviewCreate = 701,
    /// <summary>Moderate reviews (approve/reject/delete).</summary>
    ReviewModerate = 702,

    // Reporting Module Permissions (800-899)
    /// <summary>View dashboard reports.</summary>
    ReportDashboard = 800,
    /// <summary>View sales reports.</summary>
    ReportSales = 801,
    /// <summary>View revenue reports.</summary>
    ReportRevenue = 802,
    /// <summary>Export reports.</summary>
    ReportExport = 803,

    // Category Management Module Permissions (900-999)
    /// <summary>View categories.</summary>
    CategoryView = 900,
    /// <summary>Create categories.</summary>
    CategoryCreate = 901,
    /// <summary>Edit categories.</summary>
    CategoryEdit = 902,
    /// <summary>Delete categories.</summary>
    CategoryDelete = 903,

    // Platform Settings Module Permissions (1000-1099)
    /// <summary>View platform settings.</summary>
    SettingsView = 1000,
    /// <summary>Edit platform settings.</summary>
    SettingsEdit = 1001,
    /// <summary>Manage VAT rules.</summary>
    VatManage = 1002,
    /// <summary>Manage currencies.</summary>
    CurrencyManage = 1003,
    /// <summary>Manage shipping providers.</summary>
    ShippingManage = 1004,

    // Compliance Module Permissions (1100-1199)
    /// <summary>View compliance data.</summary>
    ComplianceView = 1100,
    /// <summary>Review KYC submissions.</summary>
    ComplianceKycReview = 1101,
    /// <summary>Manage data processing records.</summary>
    ComplianceDataProcessing = 1102,
    /// <summary>View audit logs.</summary>
    ComplianceAuditLog = 1103,

    // RBAC Module Permissions (1200-1299)
    /// <summary>View role configurations.</summary>
    RbacView = 1200,
    /// <summary>Manage role permissions.</summary>
    RbacManage = 1201
}
