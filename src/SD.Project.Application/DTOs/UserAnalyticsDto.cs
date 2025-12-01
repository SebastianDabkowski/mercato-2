namespace SD.Project.Application.DTOs;

/// <summary>
/// User analytics metrics for registration and activity.
/// All data is aggregated and anonymised for privacy compliance.
/// </summary>
/// <param name="NewBuyerCount">Number of new buyer registrations in the period.</param>
/// <param name="NewSellerCount">Number of new seller registrations in the period.</param>
/// <param name="ActiveUserCount">Number of users who logged in during the period.</param>
/// <param name="UsersWithOrdersCount">Number of users who placed at least one order in the period.</param>
/// <param name="FromDate">Start of the reporting period.</param>
/// <param name="ToDate">End of the reporting period.</param>
/// <param name="HasData">Indicates whether any data was found for the period.</param>
/// <param name="RefreshedAt">Timestamp when the metrics were last refreshed.</param>
public record UserAnalyticsDto(
    int NewBuyerCount,
    int NewSellerCount,
    int ActiveUserCount,
    int UsersWithOrdersCount,
    DateTime FromDate,
    DateTime ToDate,
    bool HasData,
    DateTime RefreshedAt);
