namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all data exports for a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
public record GetUserDataExportsQuery(Guid UserId);

/// <summary>
/// Query to get a specific data export by ID.
/// </summary>
/// <param name="ExportId">The ID of the export.</param>
/// <param name="UserId">The ID of the user (for authorization).</param>
public record GetUserDataExportByIdQuery(Guid ExportId, Guid UserId);

/// <summary>
/// Query to download a data export.
/// </summary>
/// <param name="ExportId">The ID of the export to download.</param>
/// <param name="UserId">The ID of the user (for authorization).</param>
public record DownloadUserDataExportQuery(Guid ExportId, Guid UserId);
