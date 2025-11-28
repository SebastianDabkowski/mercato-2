using System.Text;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for product catalog import operations.
/// </summary>
public sealed class ProductImportService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImportJobRepository _importJobRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly INotificationService _notificationService;

    public ProductImportService(
        IProductRepository productRepository,
        IProductImportJobRepository importJobRepository,
        IStoreRepository storeRepository,
        INotificationService notificationService)
    {
        _productRepository = productRepository;
        _importJobRepository = importJobRepository;
        _storeRepository = storeRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Validates an import file and returns a summary of what will be created/updated.
    /// </summary>
    public async Task<ImportValidationResultDto> ValidateImportAsync(
        Guid sellerId,
        IReadOnlyList<ImportProductRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(sellerId, cancellationToken);
        if (store is null)
        {
            return ImportValidationResultDto.FileError("Store not found.");
        }

        if (rows.Count == 0)
        {
            return ImportValidationResultDto.FileError("The import file is empty.");
        }

        // Get existing products by SKU for update detection
        var skus = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Sku))
            .Select(r => r.Sku!)
            .Distinct()
            .ToList();

        var existingProducts = await _productRepository.GetBySkusAndStoreIdAsync(skus, store.Id, cancellationToken);
        var existingSkus = existingProducts
            .Where(p => p.Sku != null)
            .ToDictionary(p => p.Sku!, StringComparer.OrdinalIgnoreCase);

        var validRows = new List<ImportProductRowDto>();
        var rowErrors = new List<ImportRowErrorDto>();
        var toBeCreated = 0;
        var toBeUpdated = 0;

        foreach (var row in rows)
        {
            var errors = ValidateRow(row);
            if (errors.Count > 0)
            {
                rowErrors.Add(new ImportRowErrorDto(row.RowNumber, row.Sku, row.Name, errors));
            }
            else
            {
                validRows.Add(row);
                if (!string.IsNullOrWhiteSpace(row.Sku) && existingSkus.ContainsKey(row.Sku!))
                {
                    toBeUpdated++;
                }
                else
                {
                    toBeCreated++;
                }
            }
        }

        if (validRows.Count == 0)
        {
            return ImportValidationResultDto.PartialSuccess(
                rows.Count,
                0,
                0,
                0,
                validRows,
                rowErrors);
        }

        if (rowErrors.Count > 0)
        {
            return ImportValidationResultDto.PartialSuccess(
                rows.Count,
                validRows.Count,
                toBeCreated,
                toBeUpdated,
                validRows,
                rowErrors);
        }

        return ImportValidationResultDto.Success(rows.Count, toBeCreated, toBeUpdated, validRows);
    }

    /// <summary>
    /// Processes the import and creates/updates products.
    /// </summary>
    public async Task<ImportResultDto> HandleAsync(ImportProductCatalogCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get seller's store
        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return ImportResultDto.Failed("Store not found.");
        }

        if (command.Rows.Count == 0)
        {
            return ImportResultDto.Failed("No rows to import.");
        }

        // Create import job
        var importJob = new ProductImportJob(
            Guid.NewGuid(),
            store.Id,
            command.SellerId,
            command.FileName,
            command.Rows.Count);

        await _importJobRepository.AddAsync(importJob, cancellationToken);
        await _importJobRepository.SaveChangesAsync(cancellationToken);

        importJob.StartProcessing();
        _importJobRepository.Update(importJob);
        await _importJobRepository.SaveChangesAsync(cancellationToken);

        try
        {
            // Get existing products by SKU
            var skus = command.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Sku))
                .Select(r => r.Sku!)
                .Distinct()
                .ToList();

            var existingProducts = await _productRepository.GetBySkusAndStoreIdAsync(skus, store.Id, cancellationToken);
            var existingBysku = existingProducts
                .Where(p => p.Sku != null)
                .ToDictionary(p => p.Sku!, StringComparer.OrdinalIgnoreCase);

            var rowErrors = new List<ImportRowErrorDto>();
            var productsToAdd = new List<Product>();
            var createdCount = 0;
            var updatedCount = 0;

            foreach (var row in command.Rows)
            {
                var errors = ValidateRow(row);
                if (errors.Count > 0)
                {
                    rowErrors.Add(new ImportRowErrorDto(row.RowNumber, row.Sku, row.Name, errors));
                    continue;
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(row.Sku) && existingBysku.TryGetValue(row.Sku!, out var existingProduct))
                    {
                        // Update existing product
                        UpdateProduct(existingProduct, row);
                        _productRepository.Update(existingProduct);
                        updatedCount++;
                    }
                    else
                    {
                        // Create new product
                        var newProduct = CreateProduct(store.Id, row);
                        productsToAdd.Add(newProduct);
                        createdCount++;

                        // Add to dictionary if it has a SKU to handle duplicates within the same import
                        if (!string.IsNullOrWhiteSpace(row.Sku))
                        {
                            existingBysku[row.Sku!] = newProduct;
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    rowErrors.Add(new ImportRowErrorDto(row.RowNumber, row.Sku, row.Name, new[] { ex.Message }));
                }
            }

            // Batch add new products
            if (productsToAdd.Count > 0)
            {
                await _productRepository.AddRangeAsync(productsToAdd, cancellationToken);
            }

            await _productRepository.SaveChangesAsync(cancellationToken);

            // Complete the import job
            var errorReport = BuildErrorReport(rowErrors);
            importJob.Complete(createdCount + updatedCount, rowErrors.Count, createdCount, updatedCount, errorReport);
            _importJobRepository.Update(importJob);
            await _importJobRepository.SaveChangesAsync(cancellationToken);

            // Send notification
            await _notificationService.SendBulkUpdateCompletedAsync(
                command.SellerId,
                createdCount + updatedCount,
                rowErrors.Count,
                cancellationToken);

            if (rowErrors.Count > 0)
            {
                return ImportResultDto.PartialSuccess(importJob.Id, command.Rows.Count, createdCount, updatedCount, rowErrors);
            }

            return ImportResultDto.Success(importJob.Id, command.Rows.Count, createdCount, updatedCount);
        }
        catch (Exception ex)
        {
            importJob.Fail(ex.Message);
            _importJobRepository.Update(importJob);
            await _importJobRepository.SaveChangesAsync(cancellationToken);

            return ImportResultDto.Failed($"Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets import job history for a store.
    /// </summary>
    public async Task<IReadOnlyCollection<ImportJobDto>> HandleAsync(GetImportJobsByStoreIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var jobs = await _importJobRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        return jobs.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Gets a specific import job by ID.
    /// </summary>
    public async Task<ImportJobDto?> HandleAsync(GetImportJobByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var job = await _importJobRepository.GetByIdAsync(query.ImportJobId, cancellationToken);
        return job is null ? null : MapToDto(job);
    }

    /// <summary>
    /// Gets the error report for an import job.
    /// </summary>
    public async Task<string?> GetErrorReportAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        var job = await _importJobRepository.GetByIdAsync(importJobId, cancellationToken);
        return job?.ErrorReport;
    }

    private static IReadOnlyList<string> ValidateRow(ImportProductRowDto row)
    {
        var errors = new List<string>();

        // SKU is required for import matching
        if (string.IsNullOrWhiteSpace(row.Sku))
        {
            errors.Add("SKU is required.");
        }
        else if (row.Sku!.Trim().Length > 100)
        {
            errors.Add("SKU cannot exceed 100 characters.");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(row.Name))
        {
            errors.Add("Product name is required.");
        }
        else if (row.Name!.Trim().Length < 3)
        {
            errors.Add("Product name must be at least 3 characters long.");
        }
        else if (row.Name.Trim().Length > 200)
        {
            errors.Add("Product name cannot exceed 200 characters.");
        }

        // Validate price
        if (!row.Price.HasValue)
        {
            errors.Add("Price is required.");
        }
        else if (row.Price.Value <= 0)
        {
            errors.Add("Price must be greater than zero.");
        }

        // Validate currency
        if (string.IsNullOrWhiteSpace(row.Currency))
        {
            errors.Add("Currency is required.");
        }
        else if (row.Currency!.Trim().Length != 3)
        {
            errors.Add("Currency must be a valid 3-letter ISO code (e.g., USD, EUR).");
        }

        // Validate stock
        if (!row.Stock.HasValue)
        {
            errors.Add("Stock is required.");
        }
        else if (row.Stock.Value < 0)
        {
            errors.Add("Stock cannot be negative.");
        }

        // Validate category
        if (string.IsNullOrWhiteSpace(row.Category))
        {
            errors.Add("Category is required.");
        }
        else if (row.Category!.Trim().Length > 100)
        {
            errors.Add("Category cannot exceed 100 characters.");
        }

        // Validate shipping parameters
        if (row.WeightKg.HasValue && row.WeightKg.Value < 0)
        {
            errors.Add("Weight cannot be negative.");
        }

        if (row.LengthCm.HasValue && row.LengthCm.Value < 0)
        {
            errors.Add("Length cannot be negative.");
        }

        if (row.WidthCm.HasValue && row.WidthCm.Value < 0)
        {
            errors.Add("Width cannot be negative.");
        }

        if (row.HeightCm.HasValue && row.HeightCm.Value < 0)
        {
            errors.Add("Height cannot be negative.");
        }

        return errors;
    }

    private static Product CreateProduct(Guid storeId, ImportProductRowDto row)
    {
        var product = new Product(
            Guid.NewGuid(),
            storeId,
            row.Name!.Trim(),
            new Money(row.Price!.Value, row.Currency!.Trim().ToUpperInvariant()),
            row.Stock!.Value,
            row.Category!.Trim());

        product.UpdateSku(row.Sku);
        product.UpdateDescription(row.Description);
        product.UpdateShippingParameters(row.WeightKg, row.LengthCm, row.WidthCm, row.HeightCm);

        return product;
    }

    private static void UpdateProduct(Product product, ImportProductRowDto row)
    {
        product.UpdateName(row.Name!.Trim());
        product.UpdateDescription(row.Description);
        product.UpdatePrice(new Money(row.Price!.Value, row.Currency!.Trim().ToUpperInvariant()));
        product.UpdateStock(row.Stock!.Value);
        product.UpdateCategory(row.Category!.Trim());
        product.UpdateShippingParameters(row.WeightKg, row.LengthCm, row.WidthCm, row.HeightCm);
    }

    private static string? BuildErrorReport(IReadOnlyList<ImportRowErrorDto> rowErrors)
    {
        if (rowErrors.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Import Error Report");
        sb.AppendLine("===================");
        sb.AppendLine();

        foreach (var error in rowErrors)
        {
            sb.AppendLine($"Row {error.RowNumber}: SKU={error.Sku ?? "(none)"}, Name={error.ProductName ?? "(none)"}");
            foreach (var errorMessage in error.Errors)
            {
                sb.AppendLine($"  - {errorMessage}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static ImportJobDto MapToDto(ProductImportJob job)
    {
        return new ImportJobDto(
            job.Id,
            job.FileName,
            job.Status.ToString(),
            job.TotalRows,
            job.SuccessCount,
            job.FailureCount,
            job.CreatedCount,
            job.UpdatedCount,
            job.CreatedAt,
            job.CompletedAt,
            !string.IsNullOrEmpty(job.ErrorReport));
    }
}
