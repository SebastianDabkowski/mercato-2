namespace SD.Project.Domain.Repositories;

/// <summary>
/// Sort options for product reviews.
/// </summary>
public enum ReviewSortOrder
{
    /// <summary>Sort by newest first (default).</summary>
    Newest,
    /// <summary>Sort by highest rating first.</summary>
    HighestRating,
    /// <summary>Sort by lowest rating first.</summary>
    LowestRating
}
