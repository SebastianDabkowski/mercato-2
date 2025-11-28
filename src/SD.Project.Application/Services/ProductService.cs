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
    private readonly INotificationService _notificationService;

    public ProductService(IProductRepository repository, INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Handles a request to create a product and persists it.
    /// </summary>
    public async Task<ProductDto> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = new Product(Guid.NewGuid(), command.Name, new Money(command.Amount, command.Currency));
        await _repository.AddAsync(product, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _notificationService.SendProductCreatedAsync(product.Id, cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Price.Amount, product.Price.Currency, product.IsActive);
    }

    /// <summary>
    /// Retrieves all products for display.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductDto>> HandleAsync(GetAllProductsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _repository.GetAllAsync(cancellationToken);
        return products
            .Select(p => new ProductDto(p.Id, p.Name, p.Price.Amount, p.Price.Currency, p.IsActive))
            .ToArray();
    }

    /// <summary>
    /// Retrieves products for a specific store.
    /// </summary>
    public async Task<IReadOnlyCollection<ProductDto>> HandleAsync(GetProductsByStoreIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _repository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        return products
            .Select(p => new ProductDto(p.Id, p.Name, p.Price.Amount, p.Price.Currency, p.IsActive))
            .ToArray();
    }
}
