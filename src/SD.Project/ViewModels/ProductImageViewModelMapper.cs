using SD.Project.Application.DTOs;

namespace SD.Project.ViewModels;

/// <summary>
/// Extension methods for mapping DTOs to view models.
/// </summary>
public static class ProductImageViewModelMapper
{
    /// <summary>
    /// Maps a ProductImageDto to a ProductImageViewModel.
    /// </summary>
    public static ProductImageViewModel ToViewModel(this ProductImageDto dto)
    {
        return new ProductImageViewModel(
            dto.Id,
            dto.ProductId,
            dto.FileName,
            dto.ImageUrl,
            dto.ThumbnailUrl,
            dto.IsMain,
            dto.DisplayOrder,
            dto.CreatedAt);
    }

    /// <summary>
    /// Maps a collection of ProductImageDto to ProductImageViewModels.
    /// </summary>
    public static IReadOnlyCollection<ProductImageViewModel> ToViewModels(this IEnumerable<ProductImageDto> dtos)
    {
        return dtos.Select(ToViewModel).ToArray();
    }
}
