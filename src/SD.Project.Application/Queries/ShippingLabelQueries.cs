namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a shipping label by ID.
/// </summary>
public sealed record GetShippingLabelQuery(
    Guid StoreId,
    Guid LabelId);

/// <summary>
/// Query to get the active shipping label for a shipment.
/// </summary>
public sealed record GetShippingLabelByShipmentQuery(
    Guid StoreId,
    Guid ShipmentId);

/// <summary>
/// Query to download a shipping label.
/// </summary>
public sealed record DownloadShippingLabelQuery(
    Guid StoreId,
    Guid LabelId);
