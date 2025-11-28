namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of an image upload operation.
/// </summary>
public sealed record UploadProductImageResultDto
{
    public bool Success { get; init; }
    public ProductImageDto? Image { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    private UploadProductImageResultDto() { }

    public static UploadProductImageResultDto Succeeded(ProductImageDto image)
        => new() { Success = true, Image = image };

    public static UploadProductImageResultDto Failed(string error)
        => new() { Success = false, Errors = new[] { error } };

    public static UploadProductImageResultDto Failed(IReadOnlyCollection<string> errors)
        => new() { Success = false, Errors = errors };
}
