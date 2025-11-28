using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for product catalog export operations.
/// </summary>
public sealed class ProductExportService
{
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;

    public ProductExportService(
        IProductRepository productRepository,
        IStoreRepository storeRepository)
    {
        _productRepository = productRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Exports products for a seller in the specified format.
    /// </summary>
    public async Task<ExportResultDto> HandleAsync(ExportProductsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(query.SellerId, cancellationToken);
        if (store is null)
        {
            return ExportResultDto.Failed("Store not found.");
        }

        // Get all products for the store
        var products = await _productRepository.GetAllByStoreIdAsync(store.Id, cancellationToken);

        // Apply filters
        var filteredProducts = ApplyFilters(products, query);

        if (filteredProducts.Count == 0)
        {
            return ExportResultDto.Failed("No products match the current filters.");
        }

        // Generate export file
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        return query.Format switch
        {
            ExportFormat.Csv => GenerateCsvExport(filteredProducts, timestamp),
            ExportFormat.Xlsx => GenerateXlsxExport(filteredProducts, timestamp),
            _ => ExportResultDto.Failed("Unsupported export format.")
        };
    }

    private static IReadOnlyCollection<Product> ApplyFilters(
        IReadOnlyCollection<Product> products,
        ExportProductsQuery query)
    {
        var filtered = products.AsEnumerable();

        // Apply search term filter (name or SKU)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.ToLowerInvariant().Contains(term) ||
                (p.Sku != null && p.Sku.ToLowerInvariant().Contains(term)));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(query.CategoryFilter))
        {
            var category = query.CategoryFilter.Trim();
            filtered = filtered.Where(p =>
                p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        // Apply active only filter
        if (query.ActiveOnly == true)
        {
            filtered = filtered.Where(p => p.Status == ProductStatus.Active);
        }

        // Exclude archived products from exports by default
        filtered = filtered.Where(p => p.Status != ProductStatus.Archived);

        return filtered.ToArray();
    }

    private static ExportResultDto GenerateCsvExport(IReadOnlyCollection<Product> products, string timestamp)
    {
        var sb = new StringBuilder();

        // Header row - matches import format for round-trip editing
        sb.AppendLine("SKU,Name,Description,Price,Currency,Stock,Category,Status,WeightKg,LengthCm,WidthCm,HeightCm");

        // Data rows
        foreach (var product in products)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvValue(product.Sku ?? string.Empty),
                EscapeCsvValue(product.Name),
                EscapeCsvValue(product.Description),
                product.Price.Amount.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsvValue(product.Price.Currency),
                product.Stock.ToString(CultureInfo.InvariantCulture),
                EscapeCsvValue(product.Category),
                EscapeCsvValue(product.Status.ToString()),
                product.WeightKg?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                product.LengthCm?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                product.WidthCm?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                product.HeightCm?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"products-export-{timestamp}.csv";

        return ExportResultDto.Success(bytes, fileName, "text/csv", products.Count);
    }

    private static ExportResultDto GenerateXlsxExport(IReadOnlyCollection<Product> products, string timestamp)
    {
        // Build shared string table first
        var sharedStrings = BuildSharedStringTable(products);

        // Generate a simple XLSX file using the Open XML format
        // XLSX is a ZIP archive containing XML files
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Add [Content_Types].xml
            AddContentTypes(archive);

            // Add _rels/.rels
            AddRelationships(archive);

            // Add xl/workbook.xml
            AddWorkbook(archive);

            // Add xl/_rels/workbook.xml.rels
            AddWorkbookRelationships(archive);

            // Add xl/worksheets/sheet1.xml
            AddWorksheet(archive, products, sharedStrings);

            // Add xl/sharedStrings.xml
            AddSharedStrings(archive, sharedStrings);

            // Add xl/styles.xml
            AddStyles(archive);
        }

        memoryStream.Position = 0;
        var bytes = memoryStream.ToArray();
        var fileName = $"products-export-{timestamp}.xlsx";

        return ExportResultDto.Success(bytes, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", products.Count);
    }

    private static Dictionary<string, int> BuildSharedStringTable(IReadOnlyCollection<Product> products)
    {
        var stringTable = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;

        // Add headers first
        var headers = new[] { "SKU", "Name", "Description", "Price", "Currency", "Stock", "Category", "Status", "WeightKg", "LengthCm", "WidthCm", "HeightCm" };
        foreach (var header in headers)
        {
            if (!stringTable.ContainsKey(header))
            {
                stringTable[header] = index++;
            }
        }

        // Add product string values
        foreach (var product in products)
        {
            var values = new[]
            {
                product.Sku ?? string.Empty,
                product.Name,
                product.Description,
                product.Price.Currency,
                product.Category,
                product.Status.ToString()
            };

            foreach (var value in values)
            {
                if (!stringTable.ContainsKey(value))
                {
                    stringTable[value] = index++;
                }
            }
        }

        return stringTable;
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // If the value contains comma, quote, or newline, wrap it in quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            // Escape quotes by doubling them
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    #region XLSX Generation Helpers

    private static void AddContentTypes(ZipArchive archive)
    {
        var entry = archive.CreateEntry("[Content_Types].xml");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        writer.WriteStartDocument();
        writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");

        writer.WriteStartElement("Default");
        writer.WriteAttributeString("Extension", "rels");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Default");
        writer.WriteAttributeString("Extension", "xml");
        writer.WriteAttributeString("ContentType", "application/xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", "/xl/workbook.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", "/xl/worksheets/sheet1.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", "/xl/sharedStrings.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", "/xl/styles.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void AddRelationships(ZipArchive archive)
    {
        var entry = archive.CreateEntry("_rels/.rels");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", "rId1");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
        writer.WriteAttributeString("Target", "xl/workbook.xml");
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void AddWorkbook(ZipArchive archive)
    {
        var entry = archive.CreateEntry("xl/workbook.xml");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        const string rns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", ns);
        writer.WriteAttributeString("xmlns", "r", null, rns);

        writer.WriteStartElement("sheets", ns);
        writer.WriteStartElement("sheet", ns);
        writer.WriteAttributeString("name", "Products");
        writer.WriteAttributeString("sheetId", "1");
        writer.WriteAttributeString("id", rns, "rId1");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void AddWorkbookRelationships(ZipArchive archive)
    {
        var entry = archive.CreateEntry("xl/_rels/workbook.xml.rels");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", "rId1");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
        writer.WriteAttributeString("Target", "worksheets/sheet1.xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", "rId2");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings");
        writer.WriteAttributeString("Target", "sharedStrings.xml");
        writer.WriteEndElement();

        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", "rId3");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
        writer.WriteAttributeString("Target", "styles.xml");
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void AddWorksheet(ZipArchive archive, IReadOnlyCollection<Product> products, Dictionary<string, int> sharedStrings)
    {
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", ns);
        writer.WriteStartElement("sheetData", ns);

        // Header row
        var headers = new[] { "SKU", "Name", "Description", "Price", "Currency", "Stock", "Category", "Status", "WeightKg", "LengthCm", "WidthCm", "HeightCm" };
        WriteRow(writer, ns, 1, headers.Select(h => (Value: h, IsString: true, Index: sharedStrings[h])).ToArray());

        // Data rows
        var rowIndex = 2;
        foreach (var product in products)
        {
            var cells = new (string Value, bool IsString, int StringIndex)[]
            {
                (product.Sku ?? string.Empty, true, sharedStrings[product.Sku ?? string.Empty]),
                (product.Name, true, sharedStrings[product.Name]),
                (product.Description, true, sharedStrings[product.Description]),
                (product.Price.Amount.ToString("F2", CultureInfo.InvariantCulture), false, -1),
                (product.Price.Currency, true, sharedStrings[product.Price.Currency]),
                (product.Stock.ToString(CultureInfo.InvariantCulture), false, -1),
                (product.Category, true, sharedStrings[product.Category]),
                (product.Status.ToString(), true, sharedStrings[product.Status.ToString()]),
                (product.WeightKg?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty, false, -1),
                (product.LengthCm?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty, false, -1),
                (product.WidthCm?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty, false, -1),
                (product.HeightCm?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty, false, -1)
            };

            WriteDataRow(writer, ns, rowIndex++, cells);
        }

        writer.WriteEndElement(); // sheetData
        writer.WriteEndElement(); // worksheet
        writer.WriteEndDocument();
    }

    private static void WriteRow(XmlWriter writer, string ns, int rowIndex, (string Value, bool IsString, int Index)[] cells)
    {
        writer.WriteStartElement("row", ns);
        writer.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));

        for (var i = 0; i < cells.Length; i++)
        {
            var cellRef = GetCellReference(i, rowIndex);
            writer.WriteStartElement("c", ns);
            writer.WriteAttributeString("r", cellRef);
            writer.WriteAttributeString("t", "s"); // Shared string
            writer.WriteElementString("v", ns, cells[i].Index.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteDataRow(XmlWriter writer, string ns, int rowIndex, (string Value, bool IsString, int StringIndex)[] cells)
    {
        writer.WriteStartElement("row", ns);
        writer.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));

        for (var i = 0; i < cells.Length; i++)
        {
            var cellRef = GetCellReference(i, rowIndex);
            writer.WriteStartElement("c", ns);
            writer.WriteAttributeString("r", cellRef);

            if (cells[i].IsString)
            {
                writer.WriteAttributeString("t", "s"); // Shared string
                writer.WriteElementString("v", ns, cells[i].StringIndex.ToString(CultureInfo.InvariantCulture));
            }
            else if (!string.IsNullOrEmpty(cells[i].Value))
            {
                writer.WriteElementString("v", ns, cells[i].Value);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static string GetCellReference(int colIndex, int rowIndex)
    {
        var col = string.Empty;
        var c = colIndex;
        while (c >= 0)
        {
            col = (char)('A' + c % 26) + col;
            c = c / 26 - 1;
        }
        return col + rowIndex.ToString(CultureInfo.InvariantCulture);
    }

    private static void AddSharedStrings(ZipArchive archive, Dictionary<string, int> sharedStrings)
    {
        var entry = archive.CreateEntry("xl/sharedStrings.xml");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        // Sort strings by their index to ensure correct order
        var orderedStrings = sharedStrings
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToArray();

        writer.WriteStartDocument();
        writer.WriteStartElement("sst", ns);
        writer.WriteAttributeString("count", orderedStrings.Length.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("uniqueCount", orderedStrings.Length.ToString(CultureInfo.InvariantCulture));

        foreach (var str in orderedStrings)
        {
            writer.WriteStartElement("si", ns);
            writer.WriteElementString("t", ns, str);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void AddStyles(ZipArchive archive)
    {
        var entry = archive.CreateEntry("xl/styles.xml");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        writer.WriteStartDocument();
        writer.WriteStartElement("styleSheet", ns);

        writer.WriteStartElement("fonts", ns);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("font", ns);
        writer.WriteStartElement("sz", ns);
        writer.WriteAttributeString("val", "11");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("fills", ns);
        writer.WriteAttributeString("count", "2");
        writer.WriteStartElement("fill", ns);
        writer.WriteStartElement("patternFill", ns);
        writer.WriteAttributeString("patternType", "none");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("fill", ns);
        writer.WriteStartElement("patternFill", ns);
        writer.WriteAttributeString("patternType", "gray125");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("borders", ns);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("border", ns);
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("cellStyleXfs", ns);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("xf", ns);
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("cellXfs", ns);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("xf", ns);
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    #endregion
}
