namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of a product creation operation.
/// </summary>
public sealed record CreateProductResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public ProductDto? Product { get; init; }

    public static CreateProductResultDto Succeeded(ProductDto product, string message = "Product created successfully.")
    {
        return new CreateProductResultDto
        {
            Success = true,
            Message = message,
            Product = product
        };
    }

    public static CreateProductResultDto Failed(string error)
    {
        return new CreateProductResultDto
        {
            Success = false,
            Errors = new[] { error }
        };
    }

    public static CreateProductResultDto Failed(IReadOnlyList<string> errors)
    {
        return new CreateProductResultDto
        {
            Success = false,
            Errors = errors
        };
    }
}
