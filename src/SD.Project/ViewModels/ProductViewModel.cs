namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display product data on Razor Pages.
/// </summary>
public sealed record ProductViewModel(Guid Id, string Name, decimal Amount, string Currency, bool IsActive);
