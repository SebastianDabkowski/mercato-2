namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get escrow payment by order ID.
/// </summary>
public sealed record GetEscrowByOrderIdQuery(
    Guid OrderId);

/// <summary>
/// Query to get escrow payment by ID.
/// </summary>
public sealed record GetEscrowByIdQuery(
    Guid EscrowPaymentId);

/// <summary>
/// Query to get seller's escrow balance summary.
/// </summary>
public sealed record GetSellerEscrowBalanceQuery(
    Guid StoreId);

/// <summary>
/// Query to get all held escrow allocations for a seller.
/// </summary>
public sealed record GetHeldEscrowAllocationsQuery(
    Guid StoreId);

/// <summary>
/// Query to get escrow allocations eligible for payout for a seller.
/// </summary>
public sealed record GetEligiblePayoutsQuery(
    Guid StoreId);

/// <summary>
/// Query to get released escrow allocations for a seller with pagination.
/// </summary>
public sealed record GetReleasedEscrowAllocationsQuery(
    Guid StoreId,
    int Skip = 0,
    int Take = 20);
