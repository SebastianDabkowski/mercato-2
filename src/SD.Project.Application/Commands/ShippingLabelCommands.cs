namespace SD.Project.Application.Commands;

/// <summary>
/// Command to generate a shipping label for a shipment.
/// </summary>
public sealed record GenerateShippingLabelCommand(
    Guid StoreId,
    Guid ShipmentId,
    Guid GeneratedByUserId,
    string? Format = "PDF",
    string? LabelSize = "4x6");

/// <summary>
/// Command to void a shipping label.
/// </summary>
public sealed record VoidShippingLabelCommand(
    Guid StoreId,
    Guid LabelId,
    Guid VoidedByUserId,
    string? Reason = null);
