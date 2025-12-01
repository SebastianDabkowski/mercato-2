using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get photos awaiting moderation with pagination and filtering.
/// </summary>
public record GetPhotosForModerationQuery(
    PhotoModerationStatus? Status = null,
    bool? IsFlagged = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get photo moderation statistics.
/// </summary>
public record GetPhotoModerationStatsQuery;

/// <summary>
/// Query to get photo moderation audit history for a specific photo.
/// </summary>
public record GetPhotoModerationHistoryQuery(Guid PhotoId);

/// <summary>
/// Query to get photo details for moderation review.
/// </summary>
public record GetPhotoForModerationDetailsQuery(Guid PhotoId);
