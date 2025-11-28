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
        _uploadsPath = Path.Combine(webRootPath, "uploads", "products");
        _thumbnailsPath = Path.Combine(webRootPath, "uploads", "products", "thumbnails");

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

        // Generate unique file name
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var imagePath = Path.Combine(_uploadsPath, storedFileName);
        var thumbnailPath = Path.Combine(_thumbnailsPath, storedFileName);

        try
        {
            // Read the original image
            using var originalBitmap = SKBitmap.Decode(imageStream);
            if (originalBitmap == null)
            {
                throw new InvalidOperationException("Failed to decode image.");
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

        var imagePath = Path.Combine(_uploadsPath, storedFileName);
        var thumbnailPath = Path.Combine(_thumbnailsPath, storedFileName);

        TryDeleteFile(imagePath);
        TryDeleteFile(thumbnailPath);

        _logger.LogInformation("Deleted image {StoredFileName}", storedFileName);

        return Task.CompletedTask;
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
