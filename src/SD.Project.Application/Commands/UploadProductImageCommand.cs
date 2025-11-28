namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to upload a product image.
/// </summary>
public sealed record UploadProductImageCommand(
    Guid ProductId,
    Guid SellerId,
    Stream ImageStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    bool SetAsMain = false);
