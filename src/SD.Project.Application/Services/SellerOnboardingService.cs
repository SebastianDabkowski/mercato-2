using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating seller onboarding use cases.
/// </summary>
public sealed class SellerOnboardingService
{
    private readonly ISellerOnboardingRepository _onboardingRepository;
    private readonly IUserRepository _userRepository;

    public SellerOnboardingService(
        ISellerOnboardingRepository onboardingRepository,
        IUserRepository userRepository)
    {
        _onboardingRepository = onboardingRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Gets or creates the onboarding record for a seller.
    /// </summary>
    public async Task<SellerOnboardingDto?> HandleAsync(GetSellerOnboardingQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Verify user exists and is a seller
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null || user.Role != UserRole.Seller)
        {
            return null;
        }

        var onboarding = await _onboardingRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (onboarding is null)
        {
            // Create new onboarding record
            onboarding = new SellerOnboarding(query.UserId);
            await _onboardingRepository.AddAsync(onboarding, cancellationToken);
            await _onboardingRepository.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(onboarding);
    }

    /// <summary>
    /// Saves store profile step data.
    /// </summary>
    public async Task<OnboardingStepResultDto> HandleAsync(SaveStoreProfileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var onboarding = await _onboardingRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (onboarding is null)
        {
            return OnboardingStepResultDto.Failed("Onboarding record not found.");
        }

        if (onboarding.Status != OnboardingStatus.InProgress)
        {
            return OnboardingStepResultDto.Failed("Cannot update completed onboarding.");
        }

        try
        {
            onboarding.UpdateStoreProfile(
                command.StoreName,
                command.StoreDescription,
                command.StoreAddress,
                command.StoreCity,
                command.StorePostalCode,
                command.StoreCountry);

            if (command.CompleteStep)
            {
                var errors = onboarding.GetStoreProfileErrors();
                if (errors.Count > 0)
                {
                    return OnboardingStepResultDto.Failed(errors);
                }

                onboarding.CompleteStoreProfile();
            }

            await _onboardingRepository.SaveChangesAsync(cancellationToken);
            return OnboardingStepResultDto.Succeeded(command.CompleteStep ? "Store profile completed." : "Store profile saved.");
        }
        catch (InvalidOperationException ex)
        {
            return OnboardingStepResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Saves verification step data.
    /// </summary>
    public async Task<OnboardingStepResultDto> HandleAsync(SaveVerificationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var onboarding = await _onboardingRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (onboarding is null)
        {
            return OnboardingStepResultDto.Failed("Onboarding record not found.");
        }

        if (onboarding.Status != OnboardingStatus.InProgress)
        {
            return OnboardingStepResultDto.Failed("Cannot update completed onboarding.");
        }

        if (!onboarding.StoreProfileCompleted)
        {
            return OnboardingStepResultDto.Failed("Store profile must be completed first.");
        }

        try
        {
            onboarding.UpdateVerification(
                command.BusinessName,
                command.BusinessRegistrationNumber,
                command.TaxIdentificationNumber,
                command.BusinessAddress);

            if (command.CompleteStep)
            {
                var errors = onboarding.GetVerificationErrors();
                if (errors.Count > 0)
                {
                    return OnboardingStepResultDto.Failed(errors);
                }

                onboarding.CompleteVerification();
            }

            await _onboardingRepository.SaveChangesAsync(cancellationToken);
            return OnboardingStepResultDto.Succeeded(command.CompleteStep ? "Verification completed." : "Verification data saved.");
        }
        catch (InvalidOperationException ex)
        {
            return OnboardingStepResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Saves payout step data and optionally completes the onboarding.
    /// </summary>
    public async Task<OnboardingStepResultDto> HandleAsync(SavePayoutCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var onboarding = await _onboardingRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (onboarding is null)
        {
            return OnboardingStepResultDto.Failed("Onboarding record not found.");
        }

        if (onboarding.Status != OnboardingStatus.InProgress)
        {
            return OnboardingStepResultDto.Failed("Cannot update completed onboarding.");
        }

        if (!onboarding.StoreProfileCompleted)
        {
            return OnboardingStepResultDto.Failed("Store profile must be completed first.");
        }

        if (!onboarding.VerificationCompleted)
        {
            return OnboardingStepResultDto.Failed("Verification must be completed first.");
        }

        try
        {
            onboarding.UpdatePayout(
                command.BankAccountHolder,
                command.BankAccountNumber,
                command.BankName,
                command.BankSwiftCode);

            if (command.CompleteStep)
            {
                var errors = onboarding.GetPayoutErrors();
                if (errors.Count > 0)
                {
                    return OnboardingStepResultDto.Failed(errors);
                }

                onboarding.CompletePayout();
            }

            await _onboardingRepository.SaveChangesAsync(cancellationToken);

            if (command.CompleteStep)
            {
                return OnboardingStepResultDto.Succeeded("Onboarding submitted! Your seller account is now pending verification.");
            }

            return OnboardingStepResultDto.Succeeded("Payout data saved.");
        }
        catch (InvalidOperationException ex)
        {
            return OnboardingStepResultDto.Failed(ex.Message);
        }
    }

    private static SellerOnboardingDto MapToDto(SellerOnboarding onboarding)
    {
        return new SellerOnboardingDto(
            onboarding.Id,
            onboarding.UserId,
            onboarding.CurrentStep,
            onboarding.Status,
            onboarding.StoreName,
            onboarding.StoreDescription,
            onboarding.StoreAddress,
            onboarding.StoreCity,
            onboarding.StorePostalCode,
            onboarding.StoreCountry,
            onboarding.StoreProfileCompleted,
            onboarding.BusinessName,
            onboarding.BusinessRegistrationNumber,
            onboarding.TaxIdentificationNumber,
            onboarding.BusinessAddress,
            onboarding.VerificationCompleted,
            onboarding.BankAccountHolder,
            onboarding.BankAccountNumber,
            onboarding.BankName,
            onboarding.BankSwiftCode,
            onboarding.PayoutCompleted,
            onboarding.CreatedAt,
            onboarding.UpdatedAt,
            onboarding.SubmittedAt);
    }
}
