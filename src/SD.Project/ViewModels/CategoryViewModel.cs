namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display category data on Razor Pages.
/// </summary>
public sealed record CategoryViewModel(
    Guid Id,
    string Name,
    string? Description,
    string Slug,
    Guid? ParentId,
    string? ParentName,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ProductCount,
    int ChildCount)
{
    /// <summary>
    /// Gets a display-friendly status string.
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Gets the parent display name or "—" if root category.
    /// </summary>
    public string ParentDisplay => ParentName ?? "—";

    /// <summary>
    /// Gets the indentation level based on parent hierarchy.
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// Gets the name with indentation for tree display.
    /// </summary>
    public string IndentedName => Level > 0 ? new string('—', Level) + " " + Name : Name;

    /// <summary>
    /// Gets the description or a placeholder if not set.
    /// </summary>
    public string DescriptionDisplay => Description ?? "—";
}
