namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display product image data on Razor Pages.
/// </summary>
public sealed record ProductImageViewModel(
    Guid Id,
    Guid ProductId,
    string FileName,
    string ImageUrl,
    string ThumbnailUrl,
    bool IsMain,
    int DisplayOrder,
    DateTime CreatedAt);
