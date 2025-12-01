namespace SD.Project.Application.Commands;

/// <summary>
/// Command to request a data export for a user.
/// </summary>
/// <param name="UserId">The ID of the user requesting the export.</param>
/// <param name="IpAddress">The IP address of the requester (optional).</param>
/// <param name="UserAgent">The user agent string (optional).</param>
public record RequestUserDataExportCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null);
