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
                command.Name,
                new Money(command.Amount, command.Currency),
                command.Stock,
                command.Category,
                command.StoreId);

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

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Name,
            p.Price.Amount,
            p.Price.Currency,
            p.Stock,
            p.Category,
            p.Status,
            p.IsActive,
            p.CreatedAt,
            p.UpdatedAt);
    }

    private static IReadOnlyList<string> ValidateProduct(CreateProductCommand command)
    {
        var errors = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Product title is required.");
        }
        else if (command.Name.Trim().Length < 3)
        {
            errors.Add("Product title must be at least 3 characters long.");
        }
        else if (command.Name.Trim().Length > 200)
        {
            errors.Add("Product title cannot exceed 200 characters.");
        }

        // Validate price
        if (command.Amount <= 0)
        {
            errors.Add("Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            errors.Add("Currency is required.");
        }
        else if (command.Currency.Trim().Length != 3)
        {
            errors.Add("Currency must be a valid 3-letter ISO code (e.g., USD, EUR).");
        }

        // Validate stock
        if (command.Stock < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        // Validate category
        if (string.IsNullOrWhiteSpace(command.Category))
        {
            errors.Add("Category is required.");
        }
        else if (command.Category.Trim().Length > 100)
        {
            errors.Add("Category cannot exceed 100 characters.");
        }

        return errors;
    }
}
