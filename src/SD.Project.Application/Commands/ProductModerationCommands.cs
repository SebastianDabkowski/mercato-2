namespace SD.Project.Application.Commands;

/// <summary>
/// Command to approve a product for listing.
/// </summary>
public record ApproveProductCommand(Guid ProductId, Guid ModeratorId);

/// <summary>
/// Command to reject a product with a reason.
/// </summary>
public record RejectProductCommand(Guid ProductId, Guid ModeratorId, string Reason);

/// <summary>
/// Command to batch approve multiple products.
/// </summary>
public record BatchApproveProductsCommand(IReadOnlyList<Guid> ProductIds, Guid ModeratorId);

/// <summary>
/// Command to batch reject multiple products with a reason.
/// </summary>
public record BatchRejectProductsCommand(IReadOnlyList<Guid> ProductIds, Guid ModeratorId, string Reason);
