using System.Linq;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating product use cases.
/// </summary>
public sealed class ProductService
{
    private readonly IProductRepository _repository;
    private readonly IStoreRepository _storeRepository;
    private readonly INotificationService _notificationService;

    public ProductService(
        IProductRepository repository,
        IStoreRepository storeRepository,
        INotificationService notificationService)
    {
        _repository = repository;
        _storeRepository = storeRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Handles a request to create a product and persists it.
    /// </summary>
    public async Task<CreateProductResultDto> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate store exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return CreateProductResultDto.Failed("Store not found.");
        }

        // Validate required fields
        var validationErrors = ValidateProduct(command);
        if (validationErrors.Count > 0)
        {
            return CreateProductResultDto.Failed(validationErrors);
        }

        try
        {
            var product = new Product(
                Guid.NewGuid(),
                command.StoreId,
                command.Name,
                new Money(command.Amount, command.Currency),
                command.Stock,
                command.Category);

            product.UpdateDescription(command.Description);
            product.UpdateShippingParameters(command.WeightKg, command.LengthCm, command.WidthCm, command.HeightCm);

            await _repository.AddAsync(product, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await _notificationService.SendProductCreatedAsync(product.Id, cancellationToken);

            return CreateProductResultDto.Succeeded(MapToDto(product));
        }
        catch (ArgumentException ex)
        {
            return CreateProductResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves all products for display.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductDto>> HandleAsync(GetAllProductsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _repository.GetAllAsync(cancellationToken);
        return products
            .Select(MapToDto)
            .ToArray();
    }

    /// <summary>
    /// Retrieves active products for a specific store (public view).
    /// </summary>
    public async Task<IReadOnlyCollection<ProductDto>> HandleAsync(GetProductsByStoreIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _repository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        return products
            .Select(MapToDto)
            .ToArray();
    }

    /// <summary>
    /// Retrieves all products for a store including drafts (seller view).
    /// </summary>
    public async Task<IReadOnlyCollection<ProductDto>> HandleAsync(GetAllProductsByStoreIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _repository.GetAllByStoreIdAsync(query.StoreId, cancellationToken);
        return products
            .Select(MapToDto)
            .ToArray();
    }

    /// <summary>
    /// Retrieves a single product by its ID.
    /// </summary>
    public async Task<ProductDto?> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await _repository.GetByIdAsync(query.ProductId, cancellationToken);
        return product is null ? null : MapToDto(product);
    }

    /// <summary>
    /// Handles a request to update an existing product.
    /// </summary>
    public async Task<UpdateProductResultDto> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return UpdateProductResultDto.Failed("Store not found.");
        }

        // Get the product
        var product = await _repository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return UpdateProductResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return UpdateProductResultDto.Failed("You do not have permission to edit this product.");
        }

        // Check if product is archived
        if (product.IsArchived)
        {
            return UpdateProductResultDto.Failed("Cannot edit an archived product.");
        }

        // Validate required fields
        var validationErrors = ValidateUpdateProduct(command);
        if (validationErrors.Count > 0)
        {
            return UpdateProductResultDto.Failed(validationErrors);
        }

        try
        {
            product.UpdateName(command.Name.Trim());
            product.UpdateDescription(command.Description);
            product.UpdatePrice(new Money(command.Amount, command.Currency.Trim().ToUpperInvariant()));
            product.UpdateStock(command.Stock);
            product.UpdateCategory(command.Category.Trim());
            product.UpdateShippingParameters(command.WeightKg, command.LengthCm, command.WidthCm, command.HeightCm);

            _repository.Update(product);
            await _repository.SaveChangesAsync(cancellationToken);
            await _notificationService.SendProductUpdatedAsync(product.Id, cancellationToken);

            return UpdateProductResultDto.Succeeded(MapToDto(product));
        }
        catch (ArgumentException ex)
        {
            return UpdateProductResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to delete (archive) a product.
    /// </summary>
    public async Task<DeleteProductResultDto> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return DeleteProductResultDto.Failed("Store not found.");
        }

        // Get the product
        var product = await _repository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return DeleteProductResultDto.Failed("Product not found.");
        }

        // Check ownership
        if (product.StoreId != store.Id)
        {
            return DeleteProductResultDto.Failed("You do not have permission to delete this product.");
        }

        // Check if product is already archived
        if (product.IsArchived)
        {
            return DeleteProductResultDto.Failed("Product is already deleted.");
        }

        try
        {
            var archiveErrors = product.Archive();
            if (archiveErrors.Count > 0)
            {
                return DeleteProductResultDto.Failed(archiveErrors.First());
            }

            _repository.Update(product);
            await _repository.SaveChangesAsync(cancellationToken);
            await _notificationService.SendProductDeletedAsync(product.Id, cancellationToken);

            return DeleteProductResultDto.Succeeded();
        }
        catch (ArgumentException ex)
        {
            return DeleteProductResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Handles a request to change the workflow status of a product.
    /// </summary>
    public async Task<ChangeProductStatusResultDto> HandleAsync(ChangeProductStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return ChangeProductStatusResultDto.Failed("Store not found.");
        }

        // Get the product
        var product = await _repository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return ChangeProductStatusResultDto.Failed("Product not found.");
        }

        // Check ownership (unless admin override)
        if (!command.IsAdminOverride && product.StoreId != store.Id)
        {
            return ChangeProductStatusResultDto.Failed("You do not have permission to change this product's status.");
        }

        var previousStatus = product.Status.ToString();

        try
        {
            var transitionErrors = product.TransitionTo(command.TargetStatus, command.IsAdminOverride);
            if (transitionErrors.Count > 0)
            {
                return ChangeProductStatusResultDto.Failed(transitionErrors);
            }

            _repository.Update(product);
            await _repository.SaveChangesAsync(cancellationToken);
            await _notificationService.SendProductStatusChangedAsync(
                product.Id,
                previousStatus,
                product.Status.ToString(),
                cancellationToken);

            return ChangeProductStatusResultDto.Succeeded(MapToDto(product));
        }
        catch (ArgumentException ex)
        {
            return ChangeProductStatusResultDto.Failed(ex.Message);
        }
    }

    private static ProductDto MapToDto(Product p)
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
            p.HeightCm);
    }

    private static IReadOnlyList<string> ValidateProduct(CreateProductCommand command)
    {
        return ValidateProductFields(command.Name, command.Amount, command.Currency, command.Stock, command.Category,
            command.WeightKg, command.LengthCm, command.WidthCm, command.HeightCm);
    }

    private static IReadOnlyList<string> ValidateUpdateProduct(UpdateProductCommand command)
    {
        return ValidateProductFields(command.Name, command.Amount, command.Currency, command.Stock, command.Category,
            command.WeightKg, command.LengthCm, command.WidthCm, command.HeightCm);
    }

    private static IReadOnlyList<string> ValidateProductFields(string name, decimal amount, string currency, int stock, string category,
        decimal? weightKg, decimal? lengthCm, decimal? widthCm, decimal? heightCm)
    {
        var errors = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Product title is required.");
        }
        else if (name.Trim().Length < 3)
        {
            errors.Add("Product title must be at least 3 characters long.");
        }
        else if (name.Trim().Length > 200)
        {
            errors.Add("Product title cannot exceed 200 characters.");
        }

        // Validate price
        if (amount <= 0)
        {
            errors.Add("Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add("Currency is required.");
        }
        else if (currency.Trim().Length != 3)
        {
            errors.Add("Currency must be a valid 3-letter ISO code (e.g., USD, EUR).");
        }

        // Validate stock
        if (stock < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        // Validate category
        if (string.IsNullOrWhiteSpace(category))
        {
            errors.Add("Category is required.");
        }
        else if (category.Trim().Length > 100)
        {
            errors.Add("Category cannot exceed 100 characters.");
        }

        // Validate shipping parameters
        if (weightKg.HasValue && weightKg.Value < 0)
        {
            errors.Add("Weight cannot be negative.");
        }

        if (lengthCm.HasValue && lengthCm.Value < 0)
        {
            errors.Add("Length cannot be negative.");
        }

        if (widthCm.HasValue && widthCm.Value < 0)
        {
            errors.Add("Width cannot be negative.");
        }

        if (heightCm.HasValue && heightCm.Value < 0)
        {
            errors.Add("Height cannot be negative.");
        }

        return errors;
    }
}
