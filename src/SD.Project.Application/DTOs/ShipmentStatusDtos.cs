namespace SD.Project.Application.DTOs;

/// <summary>
/// Result DTO for shipment status update operations.
/// </summary>
public sealed record UpdateShipmentStatusResultDto(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? PreviousStatus = null,
    string? NewStatus = null);

/// <summary>
/// DTO representing available status transitions for a shipment.
/// </summary>
public sealed record ShipmentStatusTransitionsDto(
    string CurrentStatus,
    IReadOnlyList<string> AvailableTransitions,
    bool CanUpdateTracking,
    bool CanCancel);
