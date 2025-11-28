namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of updating a product.
/// </summary>
public sealed record UpdateProductResultDto(
    bool Success,
    ProductDto? Product,
    IReadOnlyList<string> Errors)
{
    public static UpdateProductResultDto Succeeded(ProductDto product)
        => new(true, product, Array.Empty<string>());

    public static UpdateProductResultDto Failed(string error)
        => new(false, null, new[] { error });

    public static UpdateProductResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors);
}
