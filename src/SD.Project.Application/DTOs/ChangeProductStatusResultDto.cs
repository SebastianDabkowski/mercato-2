namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of a product status change operation.
/// </summary>
public sealed record ChangeProductStatusResultDto
{
    public bool Success { get; init; }
    public ProductDto? Product { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    private ChangeProductStatusResultDto() { }

    public static ChangeProductStatusResultDto Succeeded(ProductDto product)
        => new() { Success = true, Product = product };

    public static ChangeProductStatusResultDto Failed(string error)
        => new() { Success = false, Errors = new[] { error } };

    public static ChangeProductStatusResultDto Failed(IReadOnlyList<string> errors)
        => new() { Success = false, Errors = errors };
}
