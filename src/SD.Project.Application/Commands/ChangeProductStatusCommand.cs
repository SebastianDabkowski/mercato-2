using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to change the workflow status of a product.
/// </summary>
public sealed record ChangeProductStatusCommand(
    Guid ProductId,
    Guid SellerId,
    ProductStatus TargetStatus,
    bool IsAdminOverride = false);
