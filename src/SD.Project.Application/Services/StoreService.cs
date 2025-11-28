using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating store management use cases.
/// </summary>
public sealed class StoreService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;

    public StoreService(
        IStoreRepository storeRepository,
        IUserRepository userRepository)
    {
        _storeRepository = storeRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Gets a store by seller ID.
    /// </summary>
    public async Task<StoreDto?> HandleAsync(GetStoreBySellerIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var store = await _storeRepository.GetBySellerIdAsync(query.SellerId, cancellationToken);
        return store is null ? null : MapToDto(store);
    }

    /// <summary>
    /// Gets a store by its ID.
    /// </summary>
    public async Task<StoreDto?> HandleAsync(GetStoreByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        return store is null ? null : MapToDto(store);
    }

    /// <summary>
    /// Gets a store by its URL slug.
    /// </summary>
    public async Task<StoreDto?> HandleAsync(GetStoreBySlugQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Slug))
        {
            return null;
        }

        var store = await _storeRepository.GetBySlugAsync(query.Slug, cancellationToken);
        return store is null ? null : MapToDto(store);
    }

    /// <summary>
    /// Creates a new store for a seller.
    /// </summary>
    public async Task<StoreResultDto> HandleAsync(CreateStoreCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verify seller exists and has the seller role
        var user = await _userRepository.GetByIdAsync(command.SellerId, cancellationToken);
        if (user is null || user.Role != UserRole.Seller)
        {
            return StoreResultDto.Failed("User not found or is not a seller.");
        }

        // Check if seller already has a store
        var existingStore = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (existingStore is not null)
        {
            return StoreResultDto.Failed("Seller already has a store.");
        }

        // Validate store name
        var nameValidationErrors = ValidateStoreName(command.Name);
        if (nameValidationErrors.Count > 0)
        {
            return StoreResultDto.Failed(nameValidationErrors);
        }

        // Check for store name uniqueness
        if (await _storeRepository.NameExistsAsync(command.Name, null, cancellationToken))
        {
            return StoreResultDto.Failed("A store with this name already exists. Please choose a different name.");
        }

        // Validate contact email
        if (!IsValidEmail(command.ContactEmail))
        {
            return StoreResultDto.Failed("Please provide a valid contact email address.");
        }

        // Validate website URL if provided
        if (!string.IsNullOrWhiteSpace(command.WebsiteUrl) && !IsValidUrl(command.WebsiteUrl))
        {
            return StoreResultDto.Failed("Please provide a valid website URL.");
        }

        try
        {
            var store = new Store(command.SellerId, command.Name, command.ContactEmail);
            store.UpdateDescription(command.Description);
            // Only update phone and website since contact email is already set in constructor
            if (!string.IsNullOrWhiteSpace(command.PhoneNumber) || !string.IsNullOrWhiteSpace(command.WebsiteUrl))
            {
                store.UpdateContactDetails(command.ContactEmail, command.PhoneNumber, command.WebsiteUrl);
            }

            await _storeRepository.AddAsync(store, cancellationToken);
            await _storeRepository.SaveChangesAsync(cancellationToken);

            return StoreResultDto.Succeeded(MapToDto(store), "Store created successfully.");
        }
        catch (ArgumentException ex)
        {
            return StoreResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing store profile.
    /// </summary>
    public async Task<StoreResultDto> HandleAsync(UpdateStoreCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return StoreResultDto.Failed("Store not found.");
        }

        // Validate store name
        var nameValidationErrors = ValidateStoreName(command.Name);
        if (nameValidationErrors.Count > 0)
        {
            return StoreResultDto.Failed(nameValidationErrors);
        }

        // Check for store name uniqueness (excluding current store)
        if (await _storeRepository.NameExistsAsync(command.Name, store.Id, cancellationToken))
        {
            return StoreResultDto.Failed("A store with this name already exists. Please choose a different name.");
        }

        // Validate contact email
        if (!IsValidEmail(command.ContactEmail))
        {
            return StoreResultDto.Failed("Please provide a valid contact email address.");
        }

        // Validate website URL if provided
        if (!string.IsNullOrWhiteSpace(command.WebsiteUrl) && !IsValidUrl(command.WebsiteUrl))
        {
            return StoreResultDto.Failed("Please provide a valid website URL.");
        }

        try
        {
            store.UpdateProfile(
                command.Name,
                command.Description,
                command.ContactEmail,
                command.PhoneNumber,
                command.WebsiteUrl);

            await _storeRepository.SaveChangesAsync(cancellationToken);

            return StoreResultDto.Succeeded(MapToDto(store), "Store profile updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return StoreResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates a store's logo.
    /// </summary>
    public async Task<StoreResultDto> HandleAsync(UpdateStoreLogoCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var store = await _storeRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (store is null)
        {
            return StoreResultDto.Failed("Store not found.");
        }

        store.UpdateLogoUrl(command.LogoUrl);
        await _storeRepository.SaveChangesAsync(cancellationToken);

        return StoreResultDto.Succeeded(MapToDto(store), "Store logo updated successfully.");
    }

    private static StoreDto MapToDto(Store store)
    {
        return new StoreDto(
            store.Id,
            store.SellerId,
            store.Name,
            store.Slug,
            store.LogoUrl,
            store.Description,
            store.ContactEmail,
            store.PhoneNumber,
            store.WebsiteUrl,
            store.Status,
            store.CreatedAt,
            store.UpdatedAt);
    }

    private static IReadOnlyList<string> ValidateStoreName(string name)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Store name is required.");
        }
        else if (name.Trim().Length < 3)
        {
            errors.Add("Store name must be at least 3 characters long.");
        }
        else if (name.Trim().Length > 100)
        {
            errors.Add("Store name cannot exceed 100 characters.");
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
