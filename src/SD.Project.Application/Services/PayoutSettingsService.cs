using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating payout settings use cases.
/// </summary>
public sealed class PayoutSettingsService
{
    private readonly IPayoutSettingsRepository _payoutSettingsRepository;
    private readonly IUserRepository _userRepository;

    public PayoutSettingsService(
        IPayoutSettingsRepository payoutSettingsRepository,
        IUserRepository userRepository)
    {
        _payoutSettingsRepository = payoutSettingsRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Gets or creates the payout settings for a seller.
    /// </summary>
    public async Task<PayoutSettingsDto?> HandleAsync(GetPayoutSettingsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Verify user exists and is a seller
        var user = await _userRepository.GetByIdAsync(query.SellerId, cancellationToken);
        if (user is null || user.Role != UserRole.Seller)
        {
            return null;
        }

        var settings = await _payoutSettingsRepository.GetBySellerIdAsync(query.SellerId, cancellationToken);
        if (settings is null)
        {
            // Create new payout settings record
            settings = new PayoutSettings(query.SellerId);
            await _payoutSettingsRepository.AddAsync(settings, cancellationToken);
            await _payoutSettingsRepository.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(settings);
    }

    /// <summary>
    /// Saves bank transfer payout configuration.
    /// </summary>
    public async Task<PayoutSettingsResultDto> HandleAsync(SaveBankTransferPayoutCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _userRepository.GetByIdAsync(command.SellerId, cancellationToken);
        if (user is null || user.Role != UserRole.Seller)
        {
            return PayoutSettingsResultDto.Failed("User not found or is not a seller.");
        }

        var settings = await _payoutSettingsRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (settings is null)
        {
            settings = new PayoutSettings(command.SellerId);
            await _payoutSettingsRepository.AddAsync(settings, cancellationToken);
        }

        try
        {
            // If settings were previously verified, revoke verification on change
            if (settings.IsVerified)
            {
                settings.RevokeVerification();
            }

            settings.UpdateBankTransfer(
                command.BankAccountHolder,
                command.BankAccountNumber,
                command.BankName,
                command.BankSwiftCode,
                command.BankCountry);

            var errors = settings.GetBankTransferErrors();
            if (errors.Count > 0)
            {
                return PayoutSettingsResultDto.Failed(errors);
            }

            if (command.SetAsDefault)
            {
                settings.SetDefaultPayoutMethod(PayoutMethod.BankTransfer);
            }

            await _payoutSettingsRepository.SaveChangesAsync(cancellationToken);

            var message = command.SetAsDefault
                ? "Bank transfer configured and set as default payout method."
                : "Bank transfer configuration saved.";

            return PayoutSettingsResultDto.Succeeded(message, MapToDto(settings));
        }
        catch (InvalidOperationException ex)
        {
            return PayoutSettingsResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Saves SEPA payout configuration.
    /// </summary>
    public async Task<PayoutSettingsResultDto> HandleAsync(SaveSepaPayoutCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _userRepository.GetByIdAsync(command.SellerId, cancellationToken);
        if (user is null || user.Role != UserRole.Seller)
        {
            return PayoutSettingsResultDto.Failed("User not found or is not a seller.");
        }

        var settings = await _payoutSettingsRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (settings is null)
        {
            settings = new PayoutSettings(command.SellerId);
            await _payoutSettingsRepository.AddAsync(settings, cancellationToken);
        }

        try
        {
            // If settings were previously verified, revoke verification on change
            if (settings.IsVerified)
            {
                settings.RevokeVerification();
            }

            settings.UpdateSepa(command.Iban, command.Bic);

            var errors = settings.GetSepaErrors();
            if (errors.Count > 0)
            {
                return PayoutSettingsResultDto.Failed(errors);
            }

            if (command.SetAsDefault)
            {
                settings.SetDefaultPayoutMethod(PayoutMethod.Sepa);
            }

            await _payoutSettingsRepository.SaveChangesAsync(cancellationToken);

            var message = command.SetAsDefault
                ? "SEPA configured and set as default payout method."
                : "SEPA configuration saved.";

            return PayoutSettingsResultDto.Succeeded(message, MapToDto(settings));
        }
        catch (InvalidOperationException ex)
        {
            return PayoutSettingsResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Sets the default payout method.
    /// </summary>
    public async Task<PayoutSettingsResultDto> HandleAsync(SetDefaultPayoutMethodCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _userRepository.GetByIdAsync(command.SellerId, cancellationToken);
        if (user is null || user.Role != UserRole.Seller)
        {
            return PayoutSettingsResultDto.Failed("User not found or is not a seller.");
        }

        var settings = await _payoutSettingsRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (settings is null)
        {
            return PayoutSettingsResultDto.Failed("Payout settings not found. Please configure a payout method first.");
        }

        try
        {
            settings.SetDefaultPayoutMethod(command.PayoutMethod);
            await _payoutSettingsRepository.SaveChangesAsync(cancellationToken);

            var methodName = command.PayoutMethod switch
            {
                PayoutMethod.BankTransfer => "Bank Transfer",
                PayoutMethod.Sepa => "SEPA",
                _ => "None"
            };

            return PayoutSettingsResultDto.Succeeded($"{methodName} set as default payout method.", MapToDto(settings));
        }
        catch (InvalidOperationException ex)
        {
            return PayoutSettingsResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Gets validation errors for incomplete payout configuration.
    /// Used during onboarding or payout processing.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetConfigurationErrorsAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var settings = await _payoutSettingsRepository.GetBySellerIdAsync(sellerId, cancellationToken);
        if (settings is null)
        {
            return new[] { "Payout configuration must be completed before transfers can be processed." };
        }

        return settings.GetConfigurationErrors();
    }

    private static PayoutSettingsDto MapToDto(PayoutSettings settings)
    {
        return new PayoutSettingsDto(
            settings.Id,
            settings.SellerId,
            settings.DefaultPayoutMethod,
            settings.BankAccountHolder,
            settings.BankAccountNumber,
            settings.BankName,
            settings.BankSwiftCode,
            settings.BankCountry,
            settings.SepaIban,
            settings.SepaBic,
            settings.IsConfigured,
            settings.IsVerified,
            settings.IsBankTransferAvailable,
            settings.IsSepaAvailable,
            settings.CreatedAt,
            settings.UpdatedAt,
            settings.VerifiedAt);
    }
}
