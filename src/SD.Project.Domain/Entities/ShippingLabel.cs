namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a generated shipping label for a shipment.
/// Labels are stored securely and associated with their shipment.
/// </summary>
public class ShippingLabel
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The shipment this label belongs to.
    /// </summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>
    /// The order this label is associated with.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The shipping provider that generated this label.
    /// </summary>
    public Guid ShippingProviderId { get; private set; }

    /// <summary>
    /// The provider's identifier for this label.
    /// </summary>
    public string? ProviderLabelId { get; private set; }

    /// <summary>
    /// The format of the label (e.g., "PDF", "ZPL", "PNG").
    /// </summary>
    public string Format { get; private set; } = default!;

    /// <summary>
    /// The size/format specification (e.g., "4x6", "A4").
    /// </summary>
    public string? LabelSize { get; private set; }

    /// <summary>
    /// The storage path or key for the label file.
    /// </summary>
    public string StoragePath { get; private set; } = default!;

    /// <summary>
    /// The MIME content type of the label file.
    /// </summary>
    public string ContentType { get; private set; } = default!;

    /// <summary>
    /// The size of the label file in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// The tracking number printed on the label.
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// The carrier name printed on the label.
    /// </summary>
    public string? CarrierName { get; private set; }

    /// <summary>
    /// URL for external label access (if provided by provider).
    /// </summary>
    public string? ExternalUrl { get; private set; }

    /// <summary>
    /// When the label was generated.
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// When the label expires (for providers that have expiration).
    /// Null means no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// When the label was last downloaded/printed.
    /// </summary>
    public DateTime? LastAccessedAt { get; private set; }

    /// <summary>
    /// Number of times the label has been downloaded/printed.
    /// </summary>
    public int AccessCount { get; private set; }

    /// <summary>
    /// Whether the label is voided/cancelled.
    /// </summary>
    public bool IsVoided { get; private set; }

    /// <summary>
    /// When the label was voided.
    /// </summary>
    public DateTime? VoidedAt { get; private set; }

    /// <summary>
    /// Reason for voiding the label.
    /// </summary>
    public string? VoidReason { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ShippingLabel()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new shipping label.
    /// </summary>
    public ShippingLabel(
        Guid shipmentId,
        Guid orderId,
        Guid shippingProviderId,
        string format,
        string storagePath,
        string contentType,
        long fileSizeBytes,
        string? providerLabelId = null,
        string? labelSize = null,
        string? trackingNumber = null,
        string? carrierName = null,
        string? externalUrl = null,
        DateTime? expiresAt = null)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (shippingProviderId == Guid.Empty)
        {
            throw new ArgumentException("Shipping provider ID is required.", nameof(shippingProviderId));
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format is required.", nameof(format));
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path is required.", nameof(storagePath));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentException("File size must be greater than zero.", nameof(fileSizeBytes));
        }

        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        OrderId = orderId;
        ShippingProviderId = shippingProviderId;
        ProviderLabelId = providerLabelId?.Trim();
        Format = format.ToUpperInvariant();
        LabelSize = labelSize?.Trim();
        StoragePath = storagePath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        TrackingNumber = trackingNumber?.Trim();
        CarrierName = carrierName?.Trim();
        ExternalUrl = externalUrl?.Trim();
        GeneratedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        AccessCount = 0;
        IsVoided = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that the label was accessed (downloaded/printed).
    /// </summary>
    public void RecordAccess()
    {
        LastAccessedAt = DateTime.UtcNow;
        AccessCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Voids/cancels the label.
    /// </summary>
    public void Void(string? reason = null)
    {
        if (IsVoided)
        {
            throw new InvalidOperationException("Label is already voided.");
        }

        IsVoided = true;
        VoidedAt = DateTime.UtcNow;
        VoidReason = reason?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the label is valid (not voided and not expired).
    /// </summary>
    public bool IsValid()
    {
        if (IsVoided)
        {
            return false;
        }

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }
}
