namespace SD.Project.ViewModels;

/// <summary>
/// View model for an item in a return/complaint request.
/// </summary>
public sealed record ReturnRequestItemViewModel(
    Guid ItemId,
    Guid OrderItemId,
    string ProductName,
    int Quantity);

/// <summary>
/// View model for buyer's return request display.
/// </summary>
public sealed record BuyerReturnRequestViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    string Reason,
    string? Comments,
    string? SellerResponse,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ReturnRequestItemViewModel> Items);

/// <summary>
/// View model for return eligibility check.
/// </summary>
public sealed record ReturnEligibilityViewModel(
    bool IsEligible,
    string? IneligibilityReason,
    DateTime? ReturnWindowEndsAt,
    bool HasExistingReturnRequest,
    string? ExistingReturnStatus);

/// <summary>
/// View model for item eligibility in return/complaint requests.
/// </summary>
public sealed record ItemEligibilityViewModel(
    Guid OrderItemId,
    string ProductName,
    int Quantity,
    bool IsEligible,
    string? IneligibilityReason,
    bool HasOpenCase,
    string? OpenCaseNumber);

/// <summary>
/// View model for seller's return request summary in list.
/// </summary>
public sealed record SellerReturnRequestSummaryViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    string CaseNumber,
    string OrderNumber,
    string Type,
    string Status,
    string BuyerName,
    string Reason,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// View model for seller's return request details.
/// </summary>
public sealed record SellerReturnRequestDetailsViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string CaseNumber,
    string OrderNumber,
    string Type,
    string Status,
    string BuyerName,
    string? BuyerEmail,
    string Reason,
    string? Comments,
    string? SellerResponse,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<SellerSubOrderItemViewModel> Items,
    IReadOnlyList<ReturnRequestItemViewModel> RequestItems);

/// <summary>
/// View model for linked refund information in buyer case details.
/// </summary>
public sealed record LinkedRefundViewModel(
    Guid RefundId,
    string Status,
    decimal Amount,
    string Currency,
    string? RefundTransactionId,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>
/// View model for buyer case (return/complaint) details with refund info.
/// </summary>
public sealed record BuyerCaseDetailsViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    string Reason,
    string? Comments,
    string? SellerResponse,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ReturnRequestItemViewModel> Items,
    IReadOnlyList<LinkedRefundViewModel>? LinkedRefunds);

/// <summary>
/// View model for buyer case summary (list view).
/// </summary>
public sealed record BuyerCaseSummaryViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    DateTime CreatedAt);

/// <summary>
/// View model for a case message.
/// </summary>
public sealed record CaseMessageViewModel(
    Guid MessageId,
    Guid SenderId,
    string SenderRole,
    string SenderName,
    string Content,
    DateTime SentAt,
    bool IsRead,
    bool IsCurrentUser);

/// <summary>
/// View model for a case message thread.
/// </summary>
public sealed record CaseMessageThreadViewModel(
    Guid ReturnRequestId,
    string CaseNumber,
    IReadOnlyList<CaseMessageViewModel> Messages,
    int UnreadCount);

/// <summary>
/// Helper class for return request status display.
/// </summary>
public static class ReturnRequestStatusHelper
{
    /// <summary>
    /// Gets the Bootstrap CSS class for a return request status badge.
    /// </summary>
    public static string GetStatusBadgeClass(string status) => status switch
    {
        "Requested" => "bg-warning",
        "Approved" => "bg-info",
        "Rejected" => "bg-danger",
        "Completed" => "bg-success",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the Bootstrap CSS class for a return request status alert.
    /// </summary>
    public static string GetStatusAlertClass(string status) => status switch
    {
        "Requested" => "alert-warning",
        "Approved" => "alert-info",
        "Rejected" => "alert-secondary",
        "Completed" => "alert-success",
        _ => "alert-secondary"
    };

    /// <summary>
    /// Gets a user-friendly display name for a return request status.
    /// </summary>
    public static string GetStatusDisplayName(string status) => status switch
    {
        "Requested" => "Pending seller review",
        _ => status
    };

    /// <summary>
    /// Gets a user-friendly display name for a return request type.
    /// </summary>
    public static string GetTypeDisplayName(string type) => type switch
    {
        "Return" => "Return Request",
        "Complaint" => "Product Issue",
        _ => type
    };

    /// <summary>
    /// Gets the Bootstrap CSS class for a message sender role badge.
    /// </summary>
    public static string GetSenderRoleBadgeClass(string senderRole) => senderRole switch
    {
        "Admin" => "bg-danger",
        "Buyer" => "bg-info",
        "Seller" => "bg-secondary",
        _ => "bg-secondary"
    };
}
