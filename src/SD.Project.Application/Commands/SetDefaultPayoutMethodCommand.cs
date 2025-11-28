using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to set the default payout method.
/// </summary>
public sealed record SetDefaultPayoutMethodCommand(
    Guid SellerId,
    PayoutMethod PayoutMethod);
