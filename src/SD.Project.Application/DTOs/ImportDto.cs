namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents a row of product data from an import file.
/// </summary>
public sealed record ImportProductRowDto(
    int RowNumber,
    string? Sku,
    string? Name,
    string? Description,
    decimal? Price,
    string? Currency,
    int? Stock,
    string? Category,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm);

/// <summary>
/// Represents a validation error for a specific row in the import file.
/// </summary>
public sealed record ImportRowErrorDto(
    int RowNumber,
    string? Sku,
    string? ProductName,
    IReadOnlyList<string> Errors);

/// <summary>
/// Represents the validation result for an import file before processing.
/// </summary>
public sealed class ImportValidationResultDto
{
    public bool IsValid { get; private init; }
    public int TotalRows { get; private init; }
    public int ValidRows { get; private init; }
    public int InvalidRows { get; private init; }
    public int ToBeCreated { get; private init; }
    public int ToBeUpdated { get; private init; }
    public IReadOnlyList<string> FileErrors { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<ImportRowErrorDto> RowErrors { get; private init; } = Array.Empty<ImportRowErrorDto>();
    public IReadOnlyList<ImportProductRowDto> ValidatedRows { get; private init; } = Array.Empty<ImportProductRowDto>();

    private ImportValidationResultDto() { }

    public static ImportValidationResultDto Success(
        int totalRows,
        int toBeCreated,
        int toBeUpdated,
        IReadOnlyList<ImportProductRowDto> validatedRows)
    {
        return new ImportValidationResultDto
        {
            IsValid = true,
            TotalRows = totalRows,
            ValidRows = totalRows,
            InvalidRows = 0,
            ToBeCreated = toBeCreated,
            ToBeUpdated = toBeUpdated,
            FileErrors = Array.Empty<string>(),
            RowErrors = Array.Empty<ImportRowErrorDto>(),
            ValidatedRows = validatedRows
        };
    }

    public static ImportValidationResultDto PartialSuccess(
        int totalRows,
        int validRows,
        int toBeCreated,
        int toBeUpdated,
        IReadOnlyList<ImportProductRowDto> validatedRows,
        IReadOnlyList<ImportRowErrorDto> rowErrors)
    {
        return new ImportValidationResultDto
        {
            IsValid = validRows > 0,
            TotalRows = totalRows,
            ValidRows = validRows,
            InvalidRows = totalRows - validRows,
            ToBeCreated = toBeCreated,
            ToBeUpdated = toBeUpdated,
            FileErrors = Array.Empty<string>(),
            RowErrors = rowErrors,
            ValidatedRows = validatedRows
        };
    }

    public static ImportValidationResultDto FileError(params string[] errors)
    {
        return new ImportValidationResultDto
        {
            IsValid = false,
            TotalRows = 0,
            ValidRows = 0,
            InvalidRows = 0,
            ToBeCreated = 0,
            ToBeUpdated = 0,
            FileErrors = errors,
            RowErrors = Array.Empty<ImportRowErrorDto>(),
            ValidatedRows = Array.Empty<ImportProductRowDto>()
        };
    }
}

/// <summary>
/// Represents the result of processing an import job.
/// </summary>
public sealed class ImportResultDto
{
    public bool IsSuccess { get; private init; }
    public Guid? ImportJobId { get; private init; }
    public int TotalRows { get; private init; }
    public int SuccessCount { get; private init; }
    public int FailureCount { get; private init; }
    public int CreatedCount { get; private init; }
    public int UpdatedCount { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<ImportRowErrorDto> RowErrors { get; private init; } = Array.Empty<ImportRowErrorDto>();

    private ImportResultDto() { }

    public static ImportResultDto Success(
        Guid importJobId,
        int totalRows,
        int createdCount,
        int updatedCount)
    {
        return new ImportResultDto
        {
            IsSuccess = true,
            ImportJobId = importJobId,
            TotalRows = totalRows,
            SuccessCount = createdCount + updatedCount,
            FailureCount = 0,
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            Errors = Array.Empty<string>(),
            RowErrors = Array.Empty<ImportRowErrorDto>()
        };
    }

    public static ImportResultDto PartialSuccess(
        Guid importJobId,
        int totalRows,
        int createdCount,
        int updatedCount,
        IReadOnlyList<ImportRowErrorDto> rowErrors)
    {
        return new ImportResultDto
        {
            IsSuccess = true,
            ImportJobId = importJobId,
            TotalRows = totalRows,
            SuccessCount = createdCount + updatedCount,
            FailureCount = rowErrors.Count,
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            Errors = Array.Empty<string>(),
            RowErrors = rowErrors
        };
    }

    public static ImportResultDto Failed(params string[] errors)
    {
        return new ImportResultDto
        {
            IsSuccess = false,
            ImportJobId = null,
            TotalRows = 0,
            SuccessCount = 0,
            FailureCount = 0,
            CreatedCount = 0,
            UpdatedCount = 0,
            Errors = errors,
            RowErrors = Array.Empty<ImportRowErrorDto>()
        };
    }

    public static ImportResultDto Failed(IReadOnlyList<string> errors)
    {
        return new ImportResultDto
        {
            IsSuccess = false,
            ImportJobId = null,
            TotalRows = 0,
            SuccessCount = 0,
            FailureCount = 0,
            CreatedCount = 0,
            UpdatedCount = 0,
            Errors = errors,
            RowErrors = Array.Empty<ImportRowErrorDto>()
        };
    }
}

/// <summary>
/// Represents a product import job summary for display.
/// </summary>
public sealed record ImportJobDto(
    Guid Id,
    string FileName,
    string Status,
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    int CreatedCount,
    int UpdatedCount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool HasErrorReport);
