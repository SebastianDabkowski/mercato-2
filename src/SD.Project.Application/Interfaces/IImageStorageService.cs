namespace SD.Project.Application.Interfaces;

/// <summary>
/// Contract for image storage operations.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Stores an image and its thumbnail.
    /// </summary>
    /// <param name="imageStream">The image data stream.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">Content type of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the stored file name and URLs.</returns>
    Task<ImageStorageResult> StoreImageAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image and its thumbnail.
    /// </summary>
    /// <param name="storedFileName">The stored file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteImageAsync(string storedFileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of storing an image.
/// </summary>
public sealed record ImageStorageResult(
    string StoredFileName,
    string ImageUrl,
    string ThumbnailUrl);
