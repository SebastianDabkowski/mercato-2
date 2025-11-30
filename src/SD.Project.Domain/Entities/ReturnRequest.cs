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
    Completed
}

/// <summary>
/// Represents a return request initiated by a buyer for a sub-order (shipment).
/// A return request can be for the entire sub-order or specific items.
/// </summary>
public class ReturnRequest
{
    public Guid Id { get; private set; }

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
        Status = ReturnRequestStatus.Requested;
        Reason = reason.Trim();
        Comments = comments?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
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
            ReturnRequestStatus.Completed => Status == ReturnRequestStatus.Approved,
            _ => false
        };
    }
}
