namespace SD.Project.Application.Queries;

/// <summary>
/// Query to export products for a store.
/// </summary>
public sealed record ExportProductsQuery(
    Guid SellerId,
    ExportFormat Format,
    string? SearchTerm = null,
    string? CategoryFilter = null,
    bool? ActiveOnly = null);

/// <summary>
/// Supported export formats.
/// </summary>
public enum ExportFormat
{
    Csv,
    Xlsx
}
