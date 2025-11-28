using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SkiaSharp;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Local file system implementation of image storage.
/// Stores images in wwwroot/uploads with thumbnails.
/// </summary>
public sealed class LocalImageStorageService : IImageStorageService
{
    private const int ThumbnailWidth = 300;
    private const int ThumbnailHeight = 300;
    private const int MaxImageWidth = 1200;
    private const int MaxImageHeight = 1200;
    private const int JpegQuality = 85;

    private readonly string _uploadsPath;
    private readonly string _thumbnailsPath;
    private readonly ILogger<LocalImageStorageService> _logger;

    public LocalImageStorageService(
        IHostEnvironment environment,
        ILogger<LocalImageStorageService> logger)
    {
        _logger = logger;
        var webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        _uploadsPath = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "products"));
        _thumbnailsPath = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "products", "thumbnails"));

        // Ensure directories exist
        Directory.CreateDirectory(_uploadsPath);
        Directory.CreateDirectory(_thumbnailsPath);
    }

    public async Task<ImageStorageResult> StoreImageAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        // Reset stream position to ensure we read from the beginning
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        // Generate unique file name with safe extension
        var extension = GetSafeExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        // Validate the stored file name to prevent path traversal
        if (!IsValidFileName(storedFileName))
        {
            throw new InvalidOperationException("Invalid file name generated.");
        }

        var imagePath = GetSafePath(_uploadsPath, storedFileName);
        var thumbnailPath = GetSafePath(_thumbnailsPath, storedFileName);

        try
        {
            // Read the original image with validation
            using var originalBitmap = SKBitmap.Decode(imageStream);
            if (originalBitmap == null || originalBitmap.Width <= 0 || originalBitmap.Height <= 0)
            {
                throw new InvalidOperationException("Failed to decode image or image has invalid dimensions.");
            }

            // Additional validation: check for reasonable image dimensions
            if (originalBitmap.Width > 10000 || originalBitmap.Height > 10000)
            {
                throw new InvalidOperationException("Image dimensions exceed maximum allowed size.");
            }

            // Resize and save the main image (if larger than max dimensions)
            using var resizedBitmap = ResizeImage(originalBitmap, MaxImageWidth, MaxImageHeight);
            await SaveImageAsync(resizedBitmap, imagePath, contentType, cancellationToken);

            // Create and save thumbnail
            using var thumbnailBitmap = CreateThumbnail(originalBitmap, ThumbnailWidth, ThumbnailHeight);
            await SaveImageAsync(thumbnailBitmap, thumbnailPath, contentType, cancellationToken);

            _logger.LogInformation("Stored image {FileName} as {StoredFileName}", fileName, storedFileName);

            return new ImageStorageResult(
                storedFileName,
                $"/uploads/products/{storedFileName}",
                $"/uploads/products/thumbnails/{storedFileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store image {FileName}", fileName);

            // Clean up on failure
            TryDeleteFile(imagePath);
            TryDeleteFile(thumbnailPath);

            throw;
        }
    }

    public Task DeleteImageAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFileName);

        // Validate the file name to prevent path traversal
        if (!IsValidFileName(storedFileName))
        {
            _logger.LogWarning("Attempted to delete file with invalid name: {FileName}", storedFileName);
            return Task.CompletedTask;
        }

        var imagePath = GetSafePath(_uploadsPath, storedFileName);
        var thumbnailPath = GetSafePath(_thumbnailsPath, storedFileName);

        TryDeleteFile(imagePath);
        TryDeleteFile(thumbnailPath);

        _logger.LogInformation("Deleted image {StoredFileName}", storedFileName);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a safe file extension from the file name.
    /// </summary>
    private static string GetSafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? ".jpg";

        // Only allow known safe extensions
        return extension switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".png" => ".png",
            ".webp" => ".webp",
            _ => ".jpg"
        };
    }

    /// <summary>
    /// Validates that a file name is safe and does not contain path traversal characters.
    /// </summary>
    private static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        // Check for path traversal attempts
        if (fileName.Contains("..") ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            fileName.Contains(':'))
        {
            return false;
        }

        // Check for invalid path characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.IndexOfAny(invalidChars) >= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Constructs a safe file path ensuring the result is within the expected directory.
    /// </summary>
    private static string GetSafePath(string basePath, string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(basePath, fileName));

        // Ensure the resulting path is within the base path (prevents path traversal)
        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file path detected.");
        }

        return fullPath;
    }

    private static SKBitmap ResizeImage(SKBitmap original, int maxWidth, int maxHeight)
    {
        var ratioX = (double)maxWidth / original.Width;
        var ratioY = (double)maxHeight / original.Height;
        var ratio = Math.Min(ratioX, ratioY);

        // Only resize if image is larger than max dimensions
        if (ratio >= 1)
        {
            return original.Copy();
        }

        var newWidth = (int)(original.Width * ratio);
        var newHeight = (int)(original.Height * ratio);

        var info = new SKImageInfo(newWidth, newHeight);
        return original.Resize(info, SKFilterQuality.High);
    }

    private static SKBitmap CreateThumbnail(SKBitmap original, int targetWidth, int targetHeight)
    {
        // Calculate aspect ratio preserving resize
        var ratioX = (double)targetWidth / original.Width;
        var ratioY = (double)targetHeight / original.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(original.Width * ratio);
        var newHeight = (int)(original.Height * ratio);

        var info = new SKImageInfo(newWidth, newHeight);
        return original.Resize(info, SKFilterQuality.Medium);
    }

    private static async Task SaveImageAsync(SKBitmap bitmap, string path, string contentType, CancellationToken cancellationToken)
    {
        var format = contentType.ToLowerInvariant() switch
        {
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, JpegQuality);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        data.SaveTo(fileStream);
        await fileStream.FlushAsync(cancellationToken);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {Path}", path);
        }
    }
}
