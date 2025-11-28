namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of creating a product variant.
/// </summary>
public sealed record CreateProductVariantResultDto(
    bool IsSuccess,
    ProductVariantDto? Variant,
    IReadOnlyList<string> Errors)
{
    public static CreateProductVariantResultDto Succeeded(ProductVariantDto variant) =>
        new(true, variant, Array.Empty<string>());

    public static CreateProductVariantResultDto Failed(string error) =>
        new(false, null, new[] { error });

    public static CreateProductVariantResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

/// <summary>
/// Result of updating a product variant.
/// </summary>
public sealed record UpdateProductVariantResultDto(
    bool IsSuccess,
    ProductVariantDto? Variant,
    IReadOnlyList<string> Errors)
{
    public static UpdateProductVariantResultDto Succeeded(ProductVariantDto variant) =>
        new(true, variant, Array.Empty<string>());

    public static UpdateProductVariantResultDto Failed(string error) =>
        new(false, null, new[] { error });

    public static UpdateProductVariantResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

/// <summary>
/// Result of deleting a product variant.
/// </summary>
public sealed record DeleteProductVariantResultDto(
    bool IsSuccess,
    IReadOnlyList<string> Errors)
{
    public static DeleteProductVariantResultDto Succeeded() =>
        new(true, Array.Empty<string>());

    public static DeleteProductVariantResultDto Failed(string error) =>
        new(false, new[] { error });
}
