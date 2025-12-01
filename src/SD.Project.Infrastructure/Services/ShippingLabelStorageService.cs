using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Local file-based implementation of shipping label storage.
/// In production, this should be replaced with cloud storage (e.g., Azure Blob, AWS S3).
/// Labels are stored in a dedicated directory structure.
/// </summary>
public sealed class ShippingLabelStorageService : IShippingLabelStorageService
{
    private readonly ILogger<ShippingLabelStorageService> _logger;
    private readonly string _basePath;

    public ShippingLabelStorageService(
        ILogger<ShippingLabelStorageService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _basePath = configuration["ShippingLabels:StoragePath"] 
            ?? Path.Combine(Path.GetTempPath(), "mercato-shipping-labels");
    }

    /// <inheritdoc />
    public async Task<StoreLabelResult> StoreLabelAsync(
        Guid shipmentId,
        byte[] labelData,
        string format,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (labelData == null || labelData.Length == 0)
            {
                return new StoreLabelResult(false, null, null, 0, "Label data is empty.");
            }

            var extension = GetFileExtension(format);
            var contentType = GetContentType(format);

            // Create directory structure: basePath/year/month/shipmentId/
            var now = DateTime.UtcNow;
            var relativePath = Path.Combine(
                now.Year.ToString(),
                now.Month.ToString("D2"),
                shipmentId.ToString("N"));

            var directoryPath = Path.Combine(_basePath, relativePath);
            Directory.CreateDirectory(directoryPath);

            // Generate unique filename
            var fileName = $"label_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(directoryPath, fileName);
            var storagePath = Path.Combine(relativePath, fileName);

            // Write the file
            await File.WriteAllBytesAsync(fullPath, labelData, cancellationToken);

            _logger.LogInformation(
                "Stored shipping label for shipment {ShipmentId} at {StoragePath} ({FileSize} bytes)",
                shipmentId, storagePath, labelData.Length);

            return new StoreLabelResult(true, storagePath, contentType, labelData.Length, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store shipping label for shipment {ShipmentId}", shipmentId);
            return new StoreLabelResult(false, null, null, 0, $"Failed to store label: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<RetrieveLabelResult> RetrieveLabelAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                return new RetrieveLabelResult(false, null, null, null, "Storage path is required.");
            }

            var fullPath = Path.Combine(_basePath, storagePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Shipping label not found at {StoragePath}", storagePath);
                return new RetrieveLabelResult(false, null, null, null, "Label not found.");
            }

            var data = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            var fileName = Path.GetFileName(storagePath);
            var extension = Path.GetExtension(storagePath);
            var contentType = GetContentTypeFromExtension(extension);

            _logger.LogInformation("Retrieved shipping label from {StoragePath}", storagePath);

            return new RetrieveLabelResult(true, data, contentType, fileName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve shipping label from {StoragePath}", storagePath);
            return new RetrieveLabelResult(false, null, null, null, $"Failed to retrieve label: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteLabelAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                return Task.FromResult(false);
            }

            var fullPath = Path.Combine(_basePath, storagePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted shipping label at {StoragePath}", storagePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete shipping label at {StoragePath}", storagePath);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> LabelExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.FromResult(false);
        }

        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    private static string GetFileExtension(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "PDF" => ".pdf",
            "ZPL" => ".zpl",
            "PNG" => ".png",
            "GIF" => ".gif",
            "EPL" => ".epl",
            _ => ".pdf"
        };
    }

    private static string GetContentType(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "PDF" => "application/pdf",
            "ZPL" => "application/x-zpl",
            "PNG" => "image/png",
            "GIF" => "image/gif",
            "EPL" => "application/x-epl",
            _ => "application/pdf"
        };
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".zpl" => "application/x-zpl",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".epl" => "application/x-epl",
            _ => "application/octet-stream"
        };
    }
}
