using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating product image use cases.
/// </summary>
public sealed class ProductImageService
{
    private const int MaxImagesPerProduct = 10;

    private readonly IProductImageRepository _imageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IImageStorageService _imageStorageService;

    public ProductImageService(
        IProductImageRepository imageRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IImageStorageService imageStorageService)
    {
        _imageRepository = imageRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _imageStorageService = imageStorageService;
    }

    /// <summary>
    /// Handles a request to upload a product image.
    /// </summary>
    public async Task<UploadProductImageResultDto> HandleAsync(UploadProductImageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate file format
        if (!ProductImage.IsAllowedContentType(command.ContentType))
        {
            return UploadProductImageResultDto.Failed(
                $"Unsupported image format. Allowed formats: {string.Join(", ", ProductImage.AllowedExtensions)}");
        }

        var extension = Path.GetExtension(command.FileName);
        if (!ProductImage.IsAllowedExtension(extension))
        {
            return UploadProductImageResultDto.Failed(
                $"Unsupported file extension. Allowed extensions: {string.Join(", ", ProductImage.AllowedExtensions)}");
        }

        // Validate file size
        if (!ProductImage.IsAllowedFileSize(command.FileSizeBytes))
        {
            var maxSizeMb = ProductImage.MaxFileSizeBytes / (1024 * 1024);
            return UploadProductImageResultDto.Failed(
                $"Image file size exceeds the maximum allowed size of {maxSizeMb} MB.");
        }

        // Validate product exists
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return UploadProductImageResultDto.Failed("Product not found.");
        }

        // Get seller's store and validate ownership
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return UploadProductImageResultDto.Failed("Store not found.");
        }

        if (product.StoreId != store.Id)
        {
            return UploadProductImageResultDto.Failed("You do not have permission to upload images to this product.");
        }

        // Check if product is archived
        if (product.IsArchived)
        {
            return UploadProductImageResultDto.Failed("Cannot upload images to an archived product.");
        }

        // Check image count limit
        var existingImageCount = await _imageRepository.GetImageCountByProductIdAsync(command.ProductId, cancellationToken);
        if (existingImageCount >= MaxImagesPerProduct)
        {
            return UploadProductImageResultDto.Failed(
                $"Maximum of {MaxImagesPerProduct} images per product reached. Please delete an image before uploading a new one.");
        }

        try
        {
            // Store the image
            var storageResult = await _imageStorageService.StoreImageAsync(
                command.ImageStream,
                command.FileName,
                command.ContentType,
                cancellationToken);

            // Determine if this should be the main image
            var isMain = command.SetAsMain || existingImageCount == 0;

            // If setting as main, clear the main flag from existing main image
            if (isMain && existingImageCount > 0)
            {
                var existingMain = await _imageRepository.GetMainImageByProductIdAsync(command.ProductId, cancellationToken);
                if (existingMain is not null)
                {
                    existingMain.ClearMainFlag();
                    _imageRepository.Update(existingMain);
                }
            }

            // Create the image entity
            var image = new ProductImage(
                Guid.NewGuid(),
                command.ProductId,
                command.FileName,
                storageResult.StoredFileName,
                command.ContentType,
                command.FileSizeBytes,
                storageResult.ImageUrl,
                storageResult.ThumbnailUrl,
                isMain,
                existingImageCount);

            await _imageRepository.AddAsync(image, cancellationToken);
            await _imageRepository.SaveChangesAsync(cancellationToken);

            return UploadProductImageResultDto.Succeeded(MapToDto(image));
        }
        catch (Exception ex)
        {
            return UploadProductImageResultDto.Failed($"Failed to upload image: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles a request to delete a product image.
    /// </summary>
    public async Task<DeleteProductImageResultDto> HandleAsync(DeleteProductImageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the image
        var image = await _imageRepository.GetByIdAsync(command.ImageId, cancellationToken);
        if (image is null)
        {
            return DeleteProductImageResultDto.Failed("Image not found.");
        }

        // Get the product to validate ownership
        var product = await _productRepository.GetByIdAsync(image.ProductId, cancellationToken);
        if (product is null)
        {
            return DeleteProductImageResultDto.Failed("Product not found.");
        }

        // Get seller's store and validate ownership
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return DeleteProductImageResultDto.Failed("Store not found.");
        }

        if (product.StoreId != store.Id)
        {
            return DeleteProductImageResultDto.Failed("You do not have permission to delete this image.");
        }

        try
        {
            // Delete from storage
            await _imageStorageService.DeleteImageAsync(image.StoredFileName, cancellationToken);

            // If this was the main image, set another image as main
            if (image.IsMain)
            {
                var otherImages = await _imageRepository.GetByProductIdAsync(image.ProductId, cancellationToken);
                var nextImage = otherImages.FirstOrDefault(i => i.Id != image.Id);
                if (nextImage is not null)
                {
                    nextImage.SetAsMain();
                    _imageRepository.Update(nextImage);
                }
            }

            // Delete from database
            _imageRepository.Delete(image);
            await _imageRepository.SaveChangesAsync(cancellationToken);

            return DeleteProductImageResultDto.Succeeded();
        }
        catch (Exception ex)
        {
            return DeleteProductImageResultDto.Failed($"Failed to delete image: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles a request to set an image as the main product image.
    /// </summary>
    public async Task<SetMainImageResultDto> HandleAsync(SetMainProductImageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the image
        var image = await _imageRepository.GetByIdAsync(command.ImageId, cancellationToken);
        if (image is null)
        {
            return SetMainImageResultDto.Failed("Image not found.");
        }

        // Get the product to validate ownership
        var product = await _productRepository.GetByIdAsync(image.ProductId, cancellationToken);
        if (product is null)
        {
            return SetMainImageResultDto.Failed("Product not found.");
        }

        // Get seller's store and validate ownership
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return SetMainImageResultDto.Failed("Store not found.");
        }

        if (product.StoreId != store.Id)
        {
            return SetMainImageResultDto.Failed("You do not have permission to modify this image.");
        }

        // Already main, nothing to do
        if (image.IsMain)
        {
            return SetMainImageResultDto.Succeeded();
        }

        try
        {
            // Clear the main flag from the current main image
            var currentMain = await _imageRepository.GetMainImageByProductIdAsync(image.ProductId, cancellationToken);
            if (currentMain is not null)
            {
                currentMain.ClearMainFlag();
                _imageRepository.Update(currentMain);
            }

            // Set this image as main
            image.SetAsMain();
            _imageRepository.Update(image);

            await _imageRepository.SaveChangesAsync(cancellationToken);

            return SetMainImageResultDto.Succeeded();
        }
        catch (Exception ex)
        {
            return SetMainImageResultDto.Failed($"Failed to set main image: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all images for a product.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductImageDto>> HandleAsync(GetProductImagesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var images = await _imageRepository.GetByProductIdAsync(query.ProductId, cancellationToken);
        return images.Select(MapToDto).ToArray();
    }

    private static ProductImageDto MapToDto(ProductImage image)
    {
        return new ProductImageDto(
            image.Id,
            image.ProductId,
            image.FileName,
            image.ImageUrl,
            image.ThumbnailUrl,
            image.IsMain,
            image.DisplayOrder,
            image.CreatedAt);
    }
}
