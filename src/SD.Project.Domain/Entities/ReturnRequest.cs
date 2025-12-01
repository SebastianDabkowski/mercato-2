namespace SD.Project.Domain.Entities;

/// <summary>
/// Status of a return request.
/// </summary>
public enum ReturnRequestStatus
{
    /// <summary>Return request submitted by buyer, awaiting seller review.</summary>
    Requested,
    /// <summary>Return request approved by seller.</summary>
    Approved,
    /// <summary>Return request rejected by seller.</summary>
    Rejected,
    /// <summary>Return completed (item received and refund processed).</summary>
    Completed,
    /// <summary>Case escalated to admin for review.</summary>
    UnderAdminReview
}

/// <summary>
/// Reason for escalating a case to admin review.
/// </summary>
public enum EscalationReason
{
    /// <summary>Buyer requested escalation after disagreeing with seller resolution.</summary>
    BuyerRequested,
    /// <summary>System automatically escalated due to SLA breach.</summary>
    SLABreach,
    /// <summary>Admin manually flagged the case for review.</summary>
    AdminFlagged
}

/// <summary>
/// Admin decision type for escalated cases.
/// </summary>
public enum AdminDecisionType
{
    /// <summary>Admin overrode seller's decision in favor of the buyer.</summary>
    OverrideSeller,
    /// <summary>Admin enforced a refund to the buyer.</summary>
    EnforceRefund,
    /// <summary>Admin closed the case without further action, upholding seller decision.</summary>
    CloseWithoutAction
}

/// <summary>
/// Type of return/complaint request.
/// </summary>
public enum ReturnRequestType
{
    /// <summary>Standard return request (e.g., changed mind, wrong size).</summary>
    Return,
    /// <summary>Product issue complaint (e.g., defective, damaged, not as described).</summary>
    Complaint
}

/// <summary>
/// Resolution type for a return/complaint case.
/// </summary>
public enum CaseResolutionType
{
    /// <summary>Full refund of the sub-order amount.</summary>
    FullRefund,
    /// <summary>Partial refund of a specific amount.</summary>
    PartialRefund,
    /// <summary>Replacement of the product(s).</summary>
    Replacement,
    /// <summary>Repair of the product(s).</summary>
    Repair,
    /// <summary>No refund or compensation provided.</summary>
    NoRefund
}

/// <summary>
/// Represents a return request initiated by a buyer for a sub-order (shipment).
/// A return request can be for the entire sub-order or specific items.
/// </summary>
public class ReturnRequest
{
    private readonly List<ReturnRequestItem> _items = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// Human-readable unique case number (e.g., "RET-20231215-001" or "CMP-20231215-001").
    /// </summary>
    public string CaseNumber { get; private set; } = default!;

    /// <summary>
    /// The order this return request is for.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The sub-order (shipment) this return request is for.
    /// </summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>
    /// The buyer who initiated the return request.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// The store/seller this return request is for.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// Type of request (Return or Complaint).
    /// </summary>
    public ReturnRequestType Type { get; private set; }

    /// <summary>
    /// Current status of the return request.
    /// </summary>
    public ReturnRequestStatus Status { get; private set; }

    /// <summary>
    /// Reason provided by the buyer for the return.
    /// </summary>
    public string Reason { get; private set; } = default!;

    /// <summary>
    /// Optional additional comments from the buyer.
    /// </summary>
    public string? Comments { get; private set; }

    /// <summary>
    /// Response message from the seller (for approval/rejection).
    /// </summary>
    public string? SellerResponse { get; private set; }

    /// <summary>
    /// When the return request was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the return request was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// When the return request was approved (if approved).
    /// </summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>
    /// When the return request was rejected (if rejected).
    /// </summary>
    public DateTime? RejectedAt { get; private set; }

    /// <summary>
    /// When the return was completed (if completed).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// The resolution type selected by the seller.
    /// </summary>
    public CaseResolutionType? ResolutionType { get; private set; }

    /// <summary>
    /// Notes or reason provided by the seller for the resolution.
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// The ID of the linked refund (if resolution involves a refund).
    /// </summary>
    public Guid? LinkedRefundId { get; private set; }

    /// <summary>
    /// When the case was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// Partial refund amount if resolution is PartialRefund.
    /// </summary>
    public decimal? PartialRefundAmount { get; private set; }

    /// <summary>
    /// When the case was escalated to admin review.
    /// </summary>
    public DateTime? EscalatedAt { get; private set; }

    /// <summary>
    /// The ID of the user who escalated the case (buyer or admin).
    /// </summary>
    public Guid? EscalatedByUserId { get; private set; }

    /// <summary>
    /// The reason for escalation.
    /// </summary>
    public EscalationReason? EscalationReasonType { get; private set; }

    /// <summary>
    /// Additional notes provided when escalating the case.
    /// </summary>
    public string? EscalationNotes { get; private set; }

    /// <summary>
    /// The ID of the admin who made the decision (if escalated).
    /// </summary>
    public Guid? AdminDecisionByUserId { get; private set; }

    /// <summary>
    /// The admin's decision type.
    /// </summary>
    public AdminDecisionType? AdminDecision { get; private set; }

    /// <summary>
    /// Notes from the admin explaining their decision.
    /// </summary>
    public string? AdminDecisionNotes { get; private set; }

    /// <summary>
    /// When the admin made their decision.
    /// </summary>
    public DateTime? AdminDecisionAt { get; private set; }

    // SLA Tracking Properties (Phase 2)

    /// <summary>
    /// Deadline for seller's first response.
    /// </summary>
    public DateTime? FirstResponseDeadline { get; private set; }

    /// <summary>
    /// Deadline for case resolution.
    /// </summary>
    public DateTime? ResolutionDeadline { get; private set; }

    /// <summary>
    /// When the seller first responded to the case.
    /// </summary>
    public DateTime? FirstRespondedAt { get; private set; }

    /// <summary>
    /// Whether an SLA has been breached.
    /// </summary>
    public bool SlaBreached { get; private set; }

    /// <summary>
    /// When the SLA was breached.
    /// </summary>
    public DateTime? SlaBreachedAt { get; private set; }

    /// <summary>
    /// The type of SLA that was breached.
    /// </summary>
    public SlaBreachType? SlaBreachType { get; private set; }

    /// <summary>
    /// Items included in this return/complaint request.
    /// </summary>
    public IReadOnlyCollection<ReturnRequestItem> Items => _items.AsReadOnly();

    private ReturnRequest()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new return request.
    /// </summary>
    public ReturnRequest(
        Guid orderId,
        Guid shipmentId,
        Guid buyerId,
        Guid storeId,
        ReturnRequestType type,
        string reason,
        string? comments = null)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Return reason is required.", nameof(reason));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        ShipmentId = shipmentId;
        BuyerId = buyerId;
        StoreId = storeId;
        Type = type;
        Status = ReturnRequestStatus.Requested;
        Reason = reason.Trim();
        Comments = comments?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Generate case number with type prefix
        var typePrefix = type == ReturnRequestType.Return ? "RET" : "CMP";
        CaseNumber = $"{typePrefix}-{CreatedAt:yyyyMMdd}-{Id.ToString()[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Creates a new return request with backwards compatibility (defaults to Return type).
    /// </summary>
    public ReturnRequest(
        Guid orderId,
        Guid shipmentId,
        Guid buyerId,
        Guid storeId,
        string reason,
        string? comments = null)
        : this(orderId, shipmentId, buyerId, storeId, ReturnRequestType.Return, reason, comments)
    {
    }

    /// <summary>
    /// Adds an item to this return/complaint request.
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <param name="productName">The product name at time of request.</param>
    /// <param name="quantity">The quantity being returned/complained about.</param>
    /// <returns>The created return request item.</returns>
    public ReturnRequestItem AddItem(Guid orderItemId, string productName, int quantity)
    {
        if (orderItemId == Guid.Empty)
        {
            throw new ArgumentException("Order item ID is required.", nameof(orderItemId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        // Check if item already added
        if (_items.Any(i => i.OrderItemId == orderItemId))
        {
            throw new InvalidOperationException($"Order item {orderItemId} is already included in this request.");
        }

        var item = new ReturnRequestItem(Id, orderItemId, productName, quantity);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    /// <summary>
    /// Loads items from persistence.
    /// </summary>
    public void LoadItems(IEnumerable<ReturnRequestItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>
    /// Approves the return request.
    /// </summary>
    /// <param name="sellerResponse">Optional message from the seller.</param>
    public void Approve(string? sellerResponse = null)
    {
        if (Status != ReturnRequestStatus.Requested)
        {
            throw new InvalidOperationException($"Cannot approve return request in status {Status}.");
        }

        Status = ReturnRequestStatus.Approved;
        SellerResponse = sellerResponse?.Trim();
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the return request.
    /// </summary>
    /// <param name="sellerResponse">Reason for rejection from the seller.</param>
    public void Reject(string sellerResponse)
    {
        if (Status != ReturnRequestStatus.Requested)
        {
            throw new InvalidOperationException($"Cannot reject return request in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(sellerResponse))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(sellerResponse));
        }

        Status = ReturnRequestStatus.Rejected;
        SellerResponse = sellerResponse.Trim();
        RejectedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the return as completed.
    /// </summary>
    public void Complete()
    {
        if (Status != ReturnRequestStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot complete return request in status {Status}. Must be approved first.");
        }

        Status = ReturnRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the return request can transition to the specified status.
    /// </summary>
    public bool CanTransitionTo(ReturnRequestStatus targetStatus)
    {
        return targetStatus switch
        {
            ReturnRequestStatus.Requested => false, // Cannot go back to requested
            ReturnRequestStatus.Approved => Status == ReturnRequestStatus.Requested,
            ReturnRequestStatus.Rejected => Status == ReturnRequestStatus.Requested,
            ReturnRequestStatus.Completed => Status == ReturnRequestStatus.Approved || Status == ReturnRequestStatus.UnderAdminReview,
            ReturnRequestStatus.UnderAdminReview => Status == ReturnRequestStatus.Requested || Status == ReturnRequestStatus.Rejected,
            _ => false
        };
    }

    /// <summary>
    /// Resolves the case with the specified resolution type and moves it to completed status.
    /// </summary>
    /// <param name="resolutionType">The type of resolution.</param>
    /// <param name="resolutionNotes">Notes or reason for the resolution.</param>
    /// <param name="partialRefundAmount">The partial refund amount if resolution is PartialRefund.</param>
    public void Resolve(CaseResolutionType resolutionType, string? resolutionNotes, decimal? partialRefundAmount = null)
    {
        if (Status != ReturnRequestStatus.Requested && Status != ReturnRequestStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot resolve case in status {Status}. Must be in Requested or Approved status.");
        }

        if (resolutionType == CaseResolutionType.PartialRefund && (!partialRefundAmount.HasValue || partialRefundAmount.Value <= 0))
        {
            throw new ArgumentException("Partial refund amount is required and must be greater than zero.", nameof(partialRefundAmount));
        }

        if (resolutionType == CaseResolutionType.NoRefund && string.IsNullOrWhiteSpace(resolutionNotes))
        {
            throw new ArgumentException("Resolution notes are required when rejecting with no refund.", nameof(resolutionNotes));
        }

        ResolutionType = resolutionType;
        ResolutionNotes = resolutionNotes?.Trim();
        PartialRefundAmount = resolutionType == CaseResolutionType.PartialRefund ? partialRefundAmount : null;
        ResolvedAt = DateTime.UtcNow;
        Status = ReturnRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links a refund to this case.
    /// </summary>
    /// <param name="refundId">The ID of the refund to link.</param>
    public void LinkRefund(Guid refundId)
    {
        if (refundId == Guid.Empty)
        {
            throw new ArgumentException("Refund ID is required.", nameof(refundId));
        }

        LinkedRefundId = refundId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the seller can still change the resolution.
    /// Resolution can be changed before a refund has been processed.
    /// </summary>
    public bool CanChangeResolution()
    {
        // Cannot change resolution if not resolved yet or if refund is already linked
        return ResolutionType.HasValue && !LinkedRefundId.HasValue;
    }

    /// <summary>
    /// Checks if a resolution requires a refund.
    /// </summary>
    public bool ResolutionRequiresRefund()
    {
        return ResolutionType == CaseResolutionType.FullRefund || ResolutionType == CaseResolutionType.PartialRefund;
    }

    /// <summary>
    /// Escalates the case to admin review.
    /// </summary>
    /// <param name="escalatedByUserId">The ID of the user initiating the escalation.</param>
    /// <param name="reason">The reason for escalation.</param>
    /// <param name="notes">Optional notes explaining the escalation.</param>
    public void Escalate(Guid escalatedByUserId, EscalationReason reason, string? notes = null)
    {
        if (escalatedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Escalated by user ID is required.", nameof(escalatedByUserId));
        }

        if (Status != ReturnRequestStatus.Requested && Status != ReturnRequestStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot escalate case in status {Status}. Must be in Requested or Rejected status.");
        }

        Status = ReturnRequestStatus.UnderAdminReview;
        EscalatedAt = DateTime.UtcNow;
        EscalatedByUserId = escalatedByUserId;
        EscalationReasonType = reason;
        EscalationNotes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records an admin decision for an escalated case.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin making the decision.</param>
    /// <param name="decision">The admin's decision.</param>
    /// <param name="decisionNotes">Notes explaining the decision.</param>
    /// <param name="resolutionType">Optional resolution type if the admin is enforcing a specific resolution.</param>
    /// <param name="partialRefundAmount">Optional partial refund amount if resolution is PartialRefund.</param>
    public void RecordAdminDecision(
        Guid adminUserId,
        AdminDecisionType decision,
        string? decisionNotes,
        CaseResolutionType? resolutionType = null,
        decimal? partialRefundAmount = null)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin user ID is required.", nameof(adminUserId));
        }

        if (Status != ReturnRequestStatus.UnderAdminReview)
        {
            throw new InvalidOperationException($"Cannot record admin decision for case in status {Status}. Must be under admin review.");
        }

        if (string.IsNullOrWhiteSpace(decisionNotes))
        {
            throw new ArgumentException("Decision notes are required.", nameof(decisionNotes));
        }

        // Validate refund-related decisions
        if (decision == AdminDecisionType.EnforceRefund && !resolutionType.HasValue)
        {
            throw new ArgumentException("Resolution type is required when enforcing a refund.", nameof(resolutionType));
        }

        if (resolutionType == CaseResolutionType.PartialRefund && (!partialRefundAmount.HasValue || partialRefundAmount.Value <= 0))
        {
            throw new ArgumentException("Partial refund amount is required and must be greater than zero.", nameof(partialRefundAmount));
        }

        AdminDecisionByUserId = adminUserId;
        AdminDecision = decision;
        AdminDecisionNotes = decisionNotes.Trim();
        AdminDecisionAt = DateTime.UtcNow;

        // Set resolution if provided
        if (resolutionType.HasValue)
        {
            ResolutionType = resolutionType.Value;
            ResolutionNotes = decisionNotes.Trim();
            PartialRefundAmount = resolutionType == CaseResolutionType.PartialRefund ? partialRefundAmount : null;
            ResolvedAt = DateTime.UtcNow;
        }

        // Complete the case after admin decision
        Status = ReturnRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the case can be escalated to admin review.
    /// </summary>
    public bool CanEscalate()
    {
        return Status == ReturnRequestStatus.Requested || Status == ReturnRequestStatus.Rejected;
    }

    /// <summary>
    /// Checks if the case is currently escalated and awaiting admin decision.
    /// </summary>
    public bool IsEscalated => Status == ReturnRequestStatus.UnderAdminReview;

    /// <summary>
    /// Checks if the case has an admin decision recorded.
    /// </summary>
    public bool HasAdminDecision => AdminDecision.HasValue;

    // SLA Tracking Methods (Phase 2)

    /// <summary>
    /// Sets SLA deadlines for this case based on configuration.
    /// </summary>
    /// <param name="firstResponseDeadline">The deadline for first response.</param>
    /// <param name="resolutionDeadline">The deadline for resolution.</param>
    public void SetSlaDeadlines(DateTime firstResponseDeadline, DateTime resolutionDeadline)
    {
        if (resolutionDeadline <= firstResponseDeadline)
        {
            throw new ArgumentException("Resolution deadline must be after first response deadline.", nameof(resolutionDeadline));
        }

        FirstResponseDeadline = firstResponseDeadline;
        ResolutionDeadline = resolutionDeadline;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records the seller's first response to this case.
    /// </summary>
    public void RecordFirstResponse()
    {
        if (FirstRespondedAt.HasValue)
        {
            return; // Already recorded
        }

        FirstRespondedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this case as having breached SLA.
    /// </summary>
    /// <param name="breachType">The type of SLA breach.</param>
    public void MarkSlaBreached(SlaBreachType breachType)
    {
        if (SlaBreached)
        {
            return; // Already breached
        }

        SlaBreached = true;
        SlaBreachedAt = DateTime.UtcNow;
        SlaBreachType = breachType;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the first response SLA is breached (deadline passed without response).
    /// </summary>
    /// <param name="currentTime">The current time to check against.</param>
    /// <returns>True if first response SLA is breached.</returns>
    public bool IsFirstResponseSlaBreached(DateTime currentTime)
    {
        if (!FirstResponseDeadline.HasValue)
        {
            return false; // No SLA configured
        }

        if (FirstRespondedAt.HasValue)
        {
            return false; // Already responded
        }

        return currentTime > FirstResponseDeadline.Value;
    }

    /// <summary>
    /// Checks if the resolution SLA is breached (deadline passed without resolution).
    /// </summary>
    /// <param name="currentTime">The current time to check against.</param>
    /// <returns>True if resolution SLA is breached.</returns>
    public bool IsResolutionSlaBreached(DateTime currentTime)
    {
        if (!ResolutionDeadline.HasValue)
        {
            return false; // No SLA configured
        }

        if (Status == ReturnRequestStatus.Completed)
        {
            return false; // Already resolved
        }

        return currentTime > ResolutionDeadline.Value;
    }

    /// <summary>
    /// Checks if this case has SLA tracking enabled.
    /// </summary>
    public bool HasSlaTracking => FirstResponseDeadline.HasValue && ResolutionDeadline.HasValue;
}
