using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller;

[RequireRole(UserRole.Seller, UserRole.Admin)]
public class ImportProductsModel : PageModel
{
    private readonly ILogger<ImportProductsModel> _logger;
    private readonly ProductImportService _importService;
    private readonly StoreService _storeService;

    // Supported file extensions
    private static readonly string[] SupportedExtensions = { ".csv" };

    public bool HasStore { get; private set; }
    public ImportValidationResultDto? ValidationResult { get; private set; }
    public ImportResultDto? ImportResult { get; private set; }
    public IReadOnlyCollection<ImportJobViewModel> ImportHistory { get; private set; } = Array.Empty<ImportJobViewModel>();

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    [BindProperty]
    public bool ConfirmImport { get; set; }

    [BindProperty]
    public string? ValidatedRowsJson { get; set; }

    [BindProperty]
    public string? FileName { get; set; }

    public ImportProductsModel(
        ILogger<ImportProductsModel> logger,
        ProductImportService importService,
        StoreService storeService)
    {
        _logger = logger;
        _importService = importService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return RedirectToPage("/Login");
        }

        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
        if (store is null)
        {
            HasStore = false;
            return RedirectToPage("/Seller/StoreSettings");
        }

        HasStore = true;

        // Load import history
        await LoadImportHistoryAsync(store.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return RedirectToPage("/Login");
        }

        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
        if (store is null)
        {
            HasStore = false;
            return RedirectToPage("/Seller/StoreSettings");
        }

        HasStore = true;
        await LoadImportHistoryAsync(store.Id);

        if (ImportFile is null || ImportFile.Length == 0)
        {
            ValidationResult = ImportValidationResultDto.FileError("Please select a file to import.");
            return Page();
        }

        // Check file extension
        var extension = Path.GetExtension(ImportFile.FileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            ValidationResult = ImportValidationResultDto.FileError($"Unsupported file type. Please upload a CSV file.");
            return Page();
        }

        try
        {
            var rows = await ParseCsvFileAsync(ImportFile);
            if (rows.Count == 0)
            {
                ValidationResult = ImportValidationResultDto.FileError("The file is empty or has no valid data rows.");
                return Page();
            }

            ValidationResult = await _importService.ValidateImportAsync(userId, rows);
            FileName = ImportFile.FileName;

            // Store validated rows for confirmation step
            if (ValidationResult.IsValid && ValidationResult.ValidatedRows.Count > 0)
            {
                ValidatedRowsJson = System.Text.Json.JsonSerializer.Serialize(ValidationResult.ValidatedRows);
            }

            _logger.LogInformation(
                "Seller {UserId} validated import file {FileName}: {TotalRows} rows, {ValidRows} valid, {InvalidRows} invalid",
                userId,
                ImportFile.FileName,
                ValidationResult.TotalRows,
                ValidationResult.ValidRows,
                ValidationResult.InvalidRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing import file {FileName}", ImportFile.FileName);
            ValidationResult = ImportValidationResultDto.FileError($"Error reading file: {ex.Message}");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return RedirectToPage("/Login");
        }

        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
        if (store is null)
        {
            HasStore = false;
            return RedirectToPage("/Seller/StoreSettings");
        }

        HasStore = true;
        await LoadImportHistoryAsync(store.Id);

        if (string.IsNullOrEmpty(ValidatedRowsJson) || string.IsNullOrEmpty(FileName))
        {
            ImportResult = ImportResultDto.Failed("No validated data to import. Please upload and validate a file first.");
            return Page();
        }

        try
        {
            var rows = System.Text.Json.JsonSerializer.Deserialize<List<ImportProductRowDto>>(ValidatedRowsJson);
            if (rows is null || rows.Count == 0)
            {
                ImportResult = ImportResultDto.Failed("No valid rows to import.");
                return Page();
            }

            var command = new ImportProductCatalogCommand(userId, FileName, rows);
            ImportResult = await _importService.HandleAsync(command);

            _logger.LogInformation(
                "Seller {UserId} completed import of {FileName}: {CreatedCount} created, {UpdatedCount} updated, {FailureCount} failed",
                userId,
                FileName,
                ImportResult.CreatedCount,
                ImportResult.UpdatedCount,
                ImportResult.FailureCount);

            // Reload import history to show the new job
            await LoadImportHistoryAsync(store.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import for seller {UserId}", userId);
            ImportResult = ImportResultDto.Failed($"Import failed: {ex.Message}");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadErrorReportAsync(Guid jobId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return RedirectToPage("/Login");
        }

        var errorReport = await _importService.GetErrorReportAsync(jobId);
        if (string.IsNullOrEmpty(errorReport))
        {
            return NotFound();
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(errorReport);
        return File(bytes, "text/plain", $"import-errors-{jobId}.txt");
    }

    public IActionResult OnGetDownloadTemplate()
    {
        var template = "SKU,Name,Description,Price,Currency,Stock,Category,WeightKg,LengthCm,WidthCm,HeightCm\n";
        template += "SKU-001,Example Product,This is a sample product description,29.99,USD,100,Electronics,0.5,20,15,10\n";
        template += "SKU-002,Another Product,Another sample description,49.99,USD,50,Clothing,0.2,30,25,5\n";

        var bytes = System.Text.Encoding.UTF8.GetBytes(template);
        return File(bytes, "text/csv", "product-import-template.csv");
    }

    private async Task LoadImportHistoryAsync(Guid storeId)
    {
        var jobs = await _importService.HandleAsync(new GetImportJobsByStoreIdQuery(storeId));
        ImportHistory = jobs.Select(j => new ImportJobViewModel(
            j.Id,
            j.FileName,
            j.Status,
            GetStatusClass(j.Status),
            j.TotalRows,
            j.SuccessCount,
            j.FailureCount,
            j.CreatedCount,
            j.UpdatedCount,
            j.CreatedAt,
            j.CompletedAt,
            j.HasErrorReport)).ToArray();
    }

    private static string GetStatusClass(string status) => status switch
    {
        "Pending" => "bg-secondary",
        "Processing" => "bg-info",
        "Completed" => "bg-success",
        "CompletedWithErrors" => "bg-warning text-dark",
        "Failed" => "bg-danger",
        "Cancelled" => "bg-dark",
        _ => "bg-secondary"
    };

    private static async Task<IReadOnlyList<ImportProductRowDto>> ParseCsvFileAsync(IFormFile file)
    {
        var rows = new List<ImportProductRowDto>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        // Read header line
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return rows;
        }

        var headers = ParseCsvLine(headerLine)
            .Select(h => h.Trim().ToLowerInvariant())
            .ToArray();

        // Find column indices
        var skuIndex = Array.FindIndex(headers, h => h == "sku");
        var nameIndex = Array.FindIndex(headers, h => h == "name" || h == "title");
        var descriptionIndex = Array.FindIndex(headers, h => h == "description");
        var priceIndex = Array.FindIndex(headers, h => h == "price");
        var currencyIndex = Array.FindIndex(headers, h => h == "currency");
        var stockIndex = Array.FindIndex(headers, h => h == "stock");
        var categoryIndex = Array.FindIndex(headers, h => h == "category");
        var weightIndex = Array.FindIndex(headers, h => h == "weightkg" || h == "weight");
        var lengthIndex = Array.FindIndex(headers, h => h == "lengthcm" || h == "length");
        var widthIndex = Array.FindIndex(headers, h => h == "widthcm" || h == "width");
        var heightIndex = Array.FindIndex(headers, h => h == "heightcm" || h == "height");

        var rowNumber = 1; // Header is row 0
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseCsvLine(line);

            var row = new ImportProductRowDto(
                rowNumber,
                GetValueOrNull(values, skuIndex),
                GetValueOrNull(values, nameIndex),
                GetValueOrNull(values, descriptionIndex),
                ParseDecimal(GetValueOrNull(values, priceIndex)),
                GetValueOrNull(values, currencyIndex),
                ParseInt(GetValueOrNull(values, stockIndex)),
                GetValueOrNull(values, categoryIndex),
                ParseDecimal(GetValueOrNull(values, weightIndex)),
                ParseDecimal(GetValueOrNull(values, lengthIndex)),
                ParseDecimal(GetValueOrNull(values, widthIndex)),
                ParseDecimal(GetValueOrNull(values, heightIndex)));

            rows.Add(row);
        }

        return rows;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentValue = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        result.Add(currentValue.ToString());
        return result.ToArray();
    }

    private static string? GetValueOrNull(string[] values, int index)
    {
        if (index < 0 || index >= values.Length)
        {
            return null;
        }

        var value = values[index].Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
