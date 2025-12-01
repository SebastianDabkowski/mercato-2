namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a shipping label for a shipment.
/// </summary>
public sealed record ShippingLabelDto(
    Guid LabelId,
    Guid ShipmentId,
    Guid OrderId,
    string Format,
    string? LabelSize,
    string? TrackingNumber,
    string? CarrierName,
    string ContentType,
    long FileSizeBytes,
    DateTime GeneratedAt,
    DateTime? ExpiresAt,
    bool IsValid,
    bool IsVoided,
    int AccessCount);

/// <summary>
/// Result DTO for label generation operations.
/// </summary>
public sealed record GenerateLabelResultDto(
    bool IsSuccess,
    ShippingLabelDto? Label,
    string? ErrorMessage);

/// <summary>
/// Result DTO for label download operations.
/// </summary>
public sealed record DownloadLabelResultDto(
    bool IsSuccess,
    byte[]? Data,
    string? ContentType,
    string? FileName,
    string? ErrorMessage);

/// <summary>
/// Result DTO for voiding a label.
/// </summary>
public sealed record VoidLabelResultDto(
    bool IsSuccess,
    string? ErrorMessage);
