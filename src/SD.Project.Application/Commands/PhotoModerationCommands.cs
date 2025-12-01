namespace SD.Project.Application.Commands;

/// <summary>
/// Command to approve a photo for display.
/// </summary>
public record ApprovePhotoCommand(Guid PhotoId, Guid ModeratorId);

/// <summary>
/// Command to remove a photo with a reason.
/// </summary>
public record RemovePhotoCommand(Guid PhotoId, Guid ModeratorId, string Reason);

/// <summary>
/// Command to flag a photo for moderation review.
/// </summary>
public record FlagPhotoCommand(Guid PhotoId, string Reason);

/// <summary>
/// Command to batch approve multiple photos.
/// </summary>
public record BatchApprovePhotosCommand(IReadOnlyList<Guid> PhotoIds, Guid ModeratorId);

/// <summary>
/// Command to batch remove multiple photos with a reason.
/// </summary>
public record BatchRemovePhotosCommand(IReadOnlyList<Guid> PhotoIds, Guid ModeratorId, string Reason);
