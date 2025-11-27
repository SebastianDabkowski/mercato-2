using SD.Project.Application.DTOs;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to create a new product.
/// </summary>
public sealed record CreateProductCommand(string Name, decimal Amount, string Currency)
{
    public ProductDto ToDto(Guid id) => new(id, Name, Amount, Currency, true);
}
