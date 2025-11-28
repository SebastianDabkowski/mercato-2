using SD.Project.Application.DTOs;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to import products from a CSV/XLS file.
/// </summary>
public sealed record ImportProductCatalogCommand(
    Guid SellerId,
    string FileName,
    IReadOnlyList<ImportProductRowDto> Rows);
