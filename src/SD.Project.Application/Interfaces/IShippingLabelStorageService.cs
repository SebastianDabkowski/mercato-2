namespace SD.Project.Application.Interfaces;

/// <summary>
/// Result of storing a shipping label.
/// </summary>
public sealed record StoreLabelResult(
    bool IsSuccess,
    string? StoragePath,
    string? ContentType,
    long FileSizeBytes,
    string? ErrorMessage);

/// <summary>
/// Result of retrieving a shipping label.
/// </summary>
public sealed record RetrieveLabelResult(
    bool IsSuccess,
    byte[]? Data,
    string? ContentType,
    string? FileName,
    string? ErrorMessage);

/// <summary>
/// Interface for secure storage of shipping labels.
/// Labels are stored encrypted and should be cleaned up according to data retention policy.
/// </summary>
public interface IShippingLabelStorageService
{
    /// <summary>
    /// Stores a shipping label securely.
    /// </summary>
    /// <param name="shipmentId">The shipment ID for organizing the label.</param>
    /// <param name="labelData">The label data (PDF, ZPL, etc.).</param>
    /// <param name="format">The format of the label (PDF, ZPL, PNG).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing storage path or error.</returns>
    Task<StoreLabelResult> StoreLabelAsync(
        Guid shipmentId,
        byte[] labelData,
        string format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a shipping label from storage.
    /// </summary>
    /// <param name="storagePath">The storage path of the label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing label data or error.</returns>
    Task<RetrieveLabelResult> RetrieveLabelAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a shipping label from storage.
    /// </summary>
    /// <param name="storagePath">The storage path of the label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteLabelAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a label exists in storage.
    /// </summary>
    /// <param name="storagePath">The storage path of the label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the label exists.</returns>
    Task<bool> LabelExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
