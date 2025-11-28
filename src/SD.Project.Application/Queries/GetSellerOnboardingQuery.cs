namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get the current onboarding state for a seller.
/// </summary>
public sealed record GetSellerOnboardingQuery(Guid UserId);
