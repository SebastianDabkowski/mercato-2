using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating external login (OAuth) use cases.
/// </summary>
public sealed class ExternalLoginService
{
    private readonly IUserRepository _userRepository;

    public ExternalLoginService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Handles an external login request from an OAuth provider.
    /// Creates a new user if one doesn't exist with the given email.
    /// </summary>
    public async Task<ExternalLoginResultDto> HandleAsync(ExternalLoginCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return ExternalLoginResultDto.Failed("Email address is required from the provider.");
        }

        if (command.Provider == ExternalLoginProvider.None)
        {
            return ExternalLoginResultDto.Failed("External login provider must be specified.");
        }

        if (string.IsNullOrWhiteSpace(command.ExternalId))
        {
            return ExternalLoginResultDto.Failed("External ID is required from the provider.");
        }

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            return ExternalLoginResultDto.Failed("First name is required from the provider.");
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            return ExternalLoginResultDto.Failed("Last name is required from the provider.");
        }

        // Validate email format
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            return ExternalLoginResultDto.Failed("Invalid email address from the provider.");
        }

        // First, check if a user exists with this external login
        var existingUserByExternalId = await _userRepository.GetByExternalLoginAsync(
            command.Provider, command.ExternalId, cancellationToken);

        if (existingUserByExternalId is not null)
        {
            // User found by external ID - check if they're a buyer
            if (existingUserByExternalId.Role != UserRole.Buyer)
            {
                return ExternalLoginResultDto.Failed("Social login is only available for buyer accounts.");
            }

            if (existingUserByExternalId.Status == UserStatus.Suspended)
            {
                return ExternalLoginResultDto.Failed("Your account has been suspended. Please contact support.");
            }

            return ExternalLoginResultDto.Succeeded(
                existingUserByExternalId.Id,
                existingUserByExternalId.Role,
                existingUserByExternalId.Email.Value,
                existingUserByExternalId.FirstName,
                isNewUser: false);
        }

        // Check if a user exists with this email
        var existingUserByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (existingUserByEmail is not null)
        {
            // User exists with this email - only buyers can use social login
            if (existingUserByEmail.Role != UserRole.Buyer)
            {
                return ExternalLoginResultDto.Failed("Social login is only available for buyer accounts. Please use email and password to sign in.");
            }

            if (existingUserByEmail.Status == UserStatus.Suspended)
            {
                return ExternalLoginResultDto.Failed("Your account has been suspended. Please contact support.");
            }

            // Link the external login to the existing account
            // Note: For security, this should ideally require additional verification,
            // but the requirement states to log in the user if email matches a buyer account
            return ExternalLoginResultDto.Succeeded(
                existingUserByEmail.Id,
                existingUserByEmail.Role,
                existingUserByEmail.Email.Value,
                existingUserByEmail.FirstName,
                isNewUser: false);
        }

        // Create a new buyer account
        var newUser = User.CreateFromExternalLogin(
            email,
            command.Provider,
            command.ExternalId,
            command.FirstName,
            command.LastName);

        await _userRepository.AddAsync(newUser, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return ExternalLoginResultDto.Succeeded(
            newUser.Id,
            newUser.Role,
            newUser.Email.Value,
            newUser.FirstName,
            isNewUser: true);
    }
}
