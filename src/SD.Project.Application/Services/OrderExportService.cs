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
/// Application service for seller order export operations.
/// </summary>
public sealed class OrderExportService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;

    public OrderExportService(
        IOrderRepository orderRepository,
        IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Exports seller's sub-orders in the specified format.
    /// </summary>
    public async Task<ExportResultDto> HandleAsync(ExportSellerSubOrdersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Parse status filter if provided
        ShipmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && 
            Enum.TryParse<ShipmentStatus>(query.Status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        // Get all shipments matching the filter criteria
        var shipmentsData = await _orderRepository.GetAllShipmentsForExportAsync(
            query.StoreId,
            statusFilter,
            query.FromDate,
            query.ToDate,
            query.BuyerSearch,
            cancellationToken);

        if (shipmentsData.Count == 0)
        {
            return ExportResultDto.Failed("No orders match the current filters.");
        }

        // Get buyer info for the orders
        var buyerIds = shipmentsData.Select(s => s.Order.BuyerId).Distinct().ToList();
        var buyers = new Dictionary<Guid, User?>();
        foreach (var buyerId in buyerIds)
        {
            buyers[buyerId] = await _userRepository.GetByIdAsync(buyerId, cancellationToken);
        }

        // Build export rows
        var exportRows = shipmentsData.Select(s =>
        {
            var buyer = buyers.GetValueOrDefault(s.Order.BuyerId);
            var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
                ? $"{buyer.FirstName} {buyer.LastName}"
                : s.Order.RecipientName;
            var buyerEmail = buyer?.Email.Value ?? string.Empty;

            // Get the first shipping method name from items (they're typically the same for a shipment)
            var shippingMethod = s.Items.FirstOrDefault()?.ShippingMethodName ?? string.Empty;

            return new OrderExportRow(
                s.Order.Id,
                s.Order.OrderNumber,
                s.Shipment.Id,
                s.Shipment.CreatedAt,
                s.Shipment.Status.ToString(),
                buyerName,
                buyerEmail,
                s.Shipment.Subtotal + s.Shipment.ShippingCost,
                s.Order.Currency,
                shippingMethod,
                s.Items.Count,
                s.Shipment.ShippedAt,
                s.Shipment.DeliveredAt);
        }).ToList();

        // Generate export file
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        return query.Format switch
        {
            ExportFormat.Csv => GenerateCsvExport(exportRows, timestamp),
            ExportFormat.Xlsx => GenerateXlsxExport(exportRows, timestamp),
            _ => ExportResultDto.Failed("Unsupported export format.")
        };
    }

    private static ExportResultDto GenerateCsvExport(IReadOnlyCollection<OrderExportRow> rows, string timestamp)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Order ID,Order Number,Sub-Order ID,Created Date,Status,Buyer Name,Buyer Email,Total Amount,Currency,Shipping Method,Item Count,Shipped Date,Delivered Date");

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvValue(row.OrderId.ToString()),
                EscapeCsvValue(row.OrderNumber),
                EscapeCsvValue(row.SubOrderId.ToString()),
                EscapeCsvValue(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                EscapeCsvValue(row.Status),
                EscapeCsvValue(row.BuyerName),
                EscapeCsvValue(row.BuyerEmail),
                row.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsvValue(row.Currency),
                EscapeCsvValue(row.ShippingMethod),
                row.ItemCount.ToString(CultureInfo.InvariantCulture),
                EscapeCsvValue(row.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty),
                EscapeCsvValue(row.DeliveredAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"orders-export-{timestamp}.csv";

        return ExportResultDto.Success(bytes, fileName, "text/csv", rows.Count);
    }

    private static ExportResultDto GenerateXlsxExport(IReadOnlyCollection<OrderExportRow> rows, string timestamp)
    {
        // Build shared string table first
        var sharedStrings = BuildSharedStringTable(rows);

        // Generate a simple XLSX file using the Open XML format
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddContentTypes(archive);
            AddRelationships(archive);
            AddWorkbook(archive);
            AddWorkbookRelationships(archive);
            AddWorksheet(archive, rows, sharedStrings);
            AddSharedStrings(archive, sharedStrings);
            AddStyles(archive);
        }

        memoryStream.Position = 0;
        var bytes = memoryStream.ToArray();
        var fileName = $"orders-export-{timestamp}.xlsx";

        return ExportResultDto.Success(bytes, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", rows.Count);
    }

    private static Dictionary<string, int> BuildSharedStringTable(IReadOnlyCollection<OrderExportRow> rows)
    {
        var stringTable = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;

        // Add headers first
        var headers = new[] { "Order ID", "Order Number", "Sub-Order ID", "Created Date", "Status", "Buyer Name", "Buyer Email", "Total Amount", "Currency", "Shipping Method", "Item Count", "Shipped Date", "Delivered Date" };
        foreach (var header in headers)
        {
            if (!stringTable.ContainsKey(header))
            {
                stringTable[header] = index++;
            }
        }

        // Add row string values
        foreach (var row in rows)
        {
            var values = new[]
            {
                row.OrderId.ToString(),
                row.OrderNumber,
                row.SubOrderId.ToString(),
                row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                row.Status,
                row.BuyerName,
                row.BuyerEmail,
                row.Currency,
                row.ShippingMethod,
                row.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
                row.DeliveredAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty
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

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
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
        writer.WriteAttributeString("name", "Orders");
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

    private static void AddWorksheet(ZipArchive archive, IReadOnlyCollection<OrderExportRow> rows, Dictionary<string, int> sharedStrings)
    {
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml");
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8 });

        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", ns);
        writer.WriteStartElement("sheetData", ns);

        // Header row
        var headers = new[] { "Order ID", "Order Number", "Sub-Order ID", "Created Date", "Status", "Buyer Name", "Buyer Email", "Total Amount", "Currency", "Shipping Method", "Item Count", "Shipped Date", "Delivered Date" };
        WriteRow(writer, ns, 1, headers.Select(h => (Value: h, IsString: true, Index: sharedStrings[h])).ToArray());

        // Data rows
        var rowIndex = 2;
        foreach (var row in rows)
        {
            var cells = new (string Value, bool IsString, int StringIndex)[]
            {
                (row.OrderId.ToString(), true, sharedStrings[row.OrderId.ToString()]),
                (row.OrderNumber, true, sharedStrings[row.OrderNumber]),
                (row.SubOrderId.ToString(), true, sharedStrings[row.SubOrderId.ToString()]),
                (row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), true, sharedStrings[row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)]),
                (row.Status, true, sharedStrings[row.Status]),
                (row.BuyerName, true, sharedStrings[row.BuyerName]),
                (row.BuyerEmail, true, sharedStrings[row.BuyerEmail]),
                (row.TotalAmount.ToString("F2", CultureInfo.InvariantCulture), false, -1),
                (row.Currency, true, sharedStrings[row.Currency]),
                (row.ShippingMethod, true, sharedStrings[row.ShippingMethod]),
                (row.ItemCount.ToString(CultureInfo.InvariantCulture), false, -1),
                (row.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty, true, sharedStrings[row.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty]),
                (row.DeliveredAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty, true, sharedStrings[row.DeliveredAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty])
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
            writer.WriteAttributeString("t", "s");
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
                writer.WriteAttributeString("t", "s");
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

    /// <summary>
    /// Internal record for order export data.
    /// </summary>
    private sealed record OrderExportRow(
        Guid OrderId,
        string OrderNumber,
        Guid SubOrderId,
        DateTime CreatedAt,
        string Status,
        string BuyerName,
        string BuyerEmail,
        decimal TotalAmount,
        string Currency,
        string ShippingMethod,
        int ItemCount,
        DateTime? ShippedAt,
        DateTime? DeliveredAt);
}
