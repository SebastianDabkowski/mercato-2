using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating product variant use cases.
/// </summary>
public sealed class ProductVariantService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductVariantAttributeDefinitionRepository _attributeDefinitionRepository;
    private readonly IStoreRepository _storeRepository;

    public ProductVariantService(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IProductVariantAttributeDefinitionRepository attributeDefinitionRepository,
        IStoreRepository storeRepository)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _attributeDefinitionRepository = attributeDefinitionRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Creates a new product variant.
    /// </summary>
    public async Task<CreateProductVariantResultDto> HandleAsync(CreateProductVariantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return CreateProductVariantResultDto.Failed("Store not found.");
        }

        // Get the product
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return CreateProductVariantResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return CreateProductVariantResultDto.Failed("You do not have permission to add variants to this product.");
        }

        // Check if product has variants enabled
        if (!product.HasVariants)
        {
            return CreateProductVariantResultDto.Failed("Variants are not enabled for this product. Enable variants first.");
        }

        // Validate command
        var validationErrors = ValidateCreateVariant(command);
        if (validationErrors.Count > 0)
        {
            return CreateProductVariantResultDto.Failed(validationErrors);
        }

        try
        {
            var variant = new ProductVariant(Guid.NewGuid(), command.ProductId, command.AttributeValues);
            variant.UpdateSku(command.Sku);
            variant.UpdateStock(command.Stock);

            if (command.PriceOverrideAmount.HasValue && !string.IsNullOrWhiteSpace(command.PriceOverrideCurrency))
            {
                variant.SetPriceOverride(new Money(command.PriceOverrideAmount.Value, command.PriceOverrideCurrency));
            }

            await _variantRepository.AddAsync(variant, cancellationToken);
            await _variantRepository.SaveChangesAsync(cancellationToken);

            return CreateProductVariantResultDto.Succeeded(MapToDto(variant));
        }
        catch (ArgumentException ex)
        {
            return CreateProductVariantResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing product variant.
    /// </summary>
    public async Task<UpdateProductVariantResultDto> HandleAsync(UpdateProductVariantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return UpdateProductVariantResultDto.Failed("Store not found.");
        }

        // Get the variant
        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return UpdateProductVariantResultDto.Failed("Variant not found.");
        }

        // Get the product to check ownership
        var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
        if (product is null)
        {
            return UpdateProductVariantResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return UpdateProductVariantResultDto.Failed("You do not have permission to update this variant.");
        }

        // Validate command
        var validationErrors = ValidateUpdateVariant(command);
        if (validationErrors.Count > 0)
        {
            return UpdateProductVariantResultDto.Failed(validationErrors);
        }

        try
        {
            variant.UpdateSku(command.Sku);
            variant.UpdateStock(command.Stock);
            variant.UpdateAttributeValues(command.AttributeValues);

            if (command.PriceOverrideAmount.HasValue && !string.IsNullOrWhiteSpace(command.PriceOverrideCurrency))
            {
                variant.SetPriceOverride(new Money(command.PriceOverrideAmount.Value, command.PriceOverrideCurrency));
            }
            else
            {
                variant.SetPriceOverride(null);
            }

            if (command.IsAvailable)
            {
                variant.MarkAsAvailable();
            }
            else
            {
                variant.MarkAsUnavailable();
            }

            _variantRepository.Update(variant);
            await _variantRepository.SaveChangesAsync(cancellationToken);

            return UpdateProductVariantResultDto.Succeeded(MapToDto(variant));
        }
        catch (ArgumentException ex)
        {
            return UpdateProductVariantResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a product variant.
    /// </summary>
    public async Task<DeleteProductVariantResultDto> HandleAsync(DeleteProductVariantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return DeleteProductVariantResultDto.Failed("Store not found.");
        }

        // Get the variant
        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return DeleteProductVariantResultDto.Failed("Variant not found.");
        }

        // Get the product to check ownership
        var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
        if (product is null)
        {
            return DeleteProductVariantResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return DeleteProductVariantResultDto.Failed("You do not have permission to delete this variant.");
        }

        try
        {
            _variantRepository.Delete(variant);
            await _variantRepository.SaveChangesAsync(cancellationToken);

            return DeleteProductVariantResultDto.Succeeded();
        }
        catch (Exception ex)
        {
            return DeleteProductVariantResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves all variants for a product.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductVariantDto>> HandleAsync(GetProductVariantsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var variants = await _variantRepository.GetByProductIdAsync(query.ProductId, cancellationToken);
        return variants.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Retrieves available variants for a product (for buyer view).
    /// </summary>
    public async Task<IReadOnlyCollection<ProductVariantDto>> HandleAsync(GetAvailableProductVariantsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var variants = await _variantRepository.GetAvailableByProductIdAsync(query.ProductId, cancellationToken);
        return variants.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Retrieves a specific variant by ID.
    /// </summary>
    public async Task<ProductVariantDto?> HandleAsync(GetProductVariantByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var variant = await _variantRepository.GetByIdAsync(query.VariantId, cancellationToken);
        return variant is null ? null : MapToDto(variant);
    }

    /// <summary>
    /// Retrieves variant attribute definitions for a product.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductVariantAttributeDefinitionDto>> HandleAsync(GetVariantAttributeDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var definitions = await _attributeDefinitionRepository.GetByProductIdAsync(query.ProductId, cancellationToken);
        return definitions.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Enables variants for a product.
    /// </summary>
    public async Task<UpdateProductResultDto> HandleAsync(EnableProductVariantsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return UpdateProductResultDto.Failed("Store not found.");
        }

        // Get the product
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return UpdateProductResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return UpdateProductResultDto.Failed("You do not have permission to modify this product.");
        }

        try
        {
            product.EnableVariants();
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync(cancellationToken);

            return UpdateProductResultDto.Succeeded(MapProductToDto(product));
        }
        catch (ArgumentException ex)
        {
            return UpdateProductResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Adds a variant attribute definition to a product.
    /// </summary>
    public async Task<CreateAttributeDefinitionResultDto> HandleAsync(AddVariantAttributeDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return CreateAttributeDefinitionResultDto.Failed("Store not found.");
        }

        // Get the product
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return CreateAttributeDefinitionResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return CreateAttributeDefinitionResultDto.Failed("You do not have permission to modify this product.");
        }

        // Validate command
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return CreateAttributeDefinitionResultDto.Failed("Attribute name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.PossibleValues))
        {
            return CreateAttributeDefinitionResultDto.Failed("Possible values are required.");
        }

        try
        {
            var definition = new ProductVariantAttributeDefinition(
                Guid.NewGuid(),
                command.ProductId,
                command.Name,
                command.PossibleValues,
                command.DisplayOrder);

            await _attributeDefinitionRepository.AddAsync(definition, cancellationToken);
            await _attributeDefinitionRepository.SaveChangesAsync(cancellationToken);

            return CreateAttributeDefinitionResultDto.Succeeded(MapToDto(definition));
        }
        catch (ArgumentException ex)
        {
            return CreateAttributeDefinitionResultDto.Failed(ex.Message);
        }
    }

    private static ProductVariantDto MapToDto(ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Stock,
            variant.PriceOverride?.Amount,
            variant.PriceOverride?.Currency,
            variant.IsAvailable,
            variant.AttributeValues,
            variant.CreatedAt,
            variant.UpdatedAt);
    }

    private static ProductVariantAttributeDefinitionDto MapToDto(ProductVariantAttributeDefinition definition)
    {
        return new ProductVariantAttributeDefinitionDto(
            definition.Id,
            definition.ProductId,
            definition.Name,
            definition.PossibleValues,
            definition.DisplayOrder,
            definition.CreatedAt,
            definition.UpdatedAt);
    }

    private static ProductDto MapProductToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price.Amount,
            p.Price.Currency,
            p.Stock,
            p.Category,
            p.Status,
            p.IsActive,
            p.CreatedAt,
            p.UpdatedAt,
            p.WeightKg,
            p.LengthCm,
            p.WidthCm,
            p.HeightCm,
            p.Sku,
            null,
            null,
            p.HasVariants);
    }

    private static IReadOnlyList<string> ValidateCreateVariant(CreateProductVariantCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.AttributeValues))
        {
            errors.Add("Attribute values are required.");
        }

        if (command.Stock < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        if (command.PriceOverrideAmount.HasValue && command.PriceOverrideAmount.Value <= 0)
        {
            errors.Add("Price override must be greater than zero.");
        }

        if (command.PriceOverrideAmount.HasValue && string.IsNullOrWhiteSpace(command.PriceOverrideCurrency))
        {
            errors.Add("Currency is required when price override is specified.");
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateUpdateVariant(UpdateProductVariantCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.AttributeValues))
        {
            errors.Add("Attribute values are required.");
        }

        if (command.Stock < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        if (command.PriceOverrideAmount.HasValue && command.PriceOverrideAmount.Value <= 0)
        {
            errors.Add("Price override must be greater than zero.");
        }

        if (command.PriceOverrideAmount.HasValue && string.IsNullOrWhiteSpace(command.PriceOverrideCurrency))
        {
            errors.Add("Currency is required when price override is specified.");
        }

        return errors;
    }
}
