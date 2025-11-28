namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get payout settings for a seller.
/// </summary>
public sealed record GetPayoutSettingsQuery(Guid SellerId);
