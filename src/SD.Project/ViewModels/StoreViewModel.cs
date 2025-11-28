namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying store information in filter dropdowns.
/// </summary>
public sealed record StoreViewModel(
    Guid Id,
    string Name,
    string Slug);
