namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents the result of a product catalog export operation.
/// </summary>
public sealed class ExportResultDto
{
    public bool IsSuccess { get; private init; }
    public byte[]? FileData { get; private init; }
    public string? FileName { get; private init; }
    public string? ContentType { get; private init; }
    public int ExportedCount { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    private ExportResultDto() { }

    public static ExportResultDto Success(byte[] fileData, string fileName, string contentType, int exportedCount)
    {
        return new ExportResultDto
        {
            IsSuccess = true,
            FileData = fileData,
            FileName = fileName,
            ContentType = contentType,
            ExportedCount = exportedCount,
            Errors = Array.Empty<string>()
        };
    }

    public static ExportResultDto Failed(params string[] errors)
    {
        return new ExportResultDto
        {
            IsSuccess = false,
            FileData = null,
            FileName = null,
            ContentType = null,
            ExportedCount = 0,
            Errors = errors
        };
    }
}
