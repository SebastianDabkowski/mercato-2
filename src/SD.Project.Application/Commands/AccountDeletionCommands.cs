namespace SD.Project.Application.Commands;

/// <summary>
/// Command to initiate an account deletion request.
/// </summary>
/// <param name="UserId">The ID of the user requesting deletion.</param>
/// <param name="IpAddress">The IP address (optional).</param>
/// <param name="UserAgent">The user agent (optional).</param>
public sealed record RequestAccountDeletionCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to confirm and execute an account deletion.
/// </summary>
/// <param name="DeletionRequestId">The ID of the deletion request to confirm.</param>
/// <param name="UserId">The ID of the user confirming deletion.</param>
/// <param name="IpAddress">The IP address (optional).</param>
/// <param name="UserAgent">The user agent (optional).</param>
public sealed record ConfirmAccountDeletionCommand(
    Guid DeletionRequestId,
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to cancel a pending account deletion request.
/// </summary>
/// <param name="DeletionRequestId">The ID of the deletion request to cancel.</param>
/// <param name="UserId">The ID of the user cancelling the request.</param>
public sealed record CancelAccountDeletionCommand(
    Guid DeletionRequestId,
    Guid UserId);
