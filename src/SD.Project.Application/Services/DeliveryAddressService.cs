using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating delivery address use cases.
/// </summary>
public sealed class DeliveryAddressService
{
    private readonly IDeliveryAddressRepository _addressRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;

    // List of countries where Mercato operates and can ship to
    private static readonly HashSet<string> AllowedCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "United States", "USA", "US",
        "Canada", "CA",
        "United Kingdom", "UK", "GB",
        "Germany", "DE",
        "France", "FR",
        "Spain", "ES",
        "Italy", "IT",
        "Netherlands", "NL",
        "Belgium", "BE",
        "Poland", "PL",
        "Australia", "AU"
    };

    public DeliveryAddressService(
        IDeliveryAddressRepository addressRepository,
        ICartRepository cartRepository,
        IProductRepository productRepository)
    {
        _addressRepository = addressRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Gets all saved addresses for a buyer or session.
    /// </summary>
    public async Task<IReadOnlyList<DeliveryAddressDto>> HandleAsync(
        GetDeliveryAddressesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<DeliveryAddress> addresses;

        if (query.BuyerId.HasValue && query.BuyerId.Value != Guid.Empty)
        {
            addresses = await _addressRepository.GetByBuyerIdAsync(query.BuyerId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(query.SessionId))
        {
            addresses = await _addressRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);
        }
        else
        {
            return Array.Empty<DeliveryAddressDto>();
        }

        return addresses.Select(MapToDto).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a specific address by ID.
    /// </summary>
    public async Task<DeliveryAddressDto?> HandleAsync(
        GetDeliveryAddressByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var address = await _addressRepository.GetByIdAsync(query.AddressId, cancellationToken);
        if (address is null)
        {
            return null;
        }

        // Verify ownership
        if (query.BuyerId.HasValue && query.BuyerId.Value != Guid.Empty)
        {
            if (address.BuyerId != query.BuyerId.Value)
            {
                return null;
            }
        }
        else if (!string.IsNullOrWhiteSpace(query.SessionId))
        {
            if (address.SessionId != query.SessionId)
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        return MapToDto(address);
    }

    /// <summary>
    /// Gets the default address for a buyer.
    /// </summary>
    public async Task<DeliveryAddressDto?> HandleAsync(
        GetDefaultDeliveryAddressQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var address = await _addressRepository.GetDefaultByBuyerIdAsync(query.BuyerId, cancellationToken);
        return address is not null ? MapToDto(address) : null;
    }

    /// <summary>
    /// Saves a new delivery address or updates an existing one.
    /// </summary>
    public async Task<SaveDeliveryAddressResultDto> HandleAsync(
        SaveDeliveryAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate address fields
        Address addressValue;
        try
        {
            addressValue = Address.Create(
                command.Street,
                command.Street2,
                command.City,
                command.State,
                command.PostalCode,
                command.Country);
        }
        catch (ArgumentException ex)
        {
            return SaveDeliveryAddressResultDto.Failed(ex.Message);
        }

        // Validate recipient name
        if (string.IsNullOrWhiteSpace(command.RecipientName))
        {
            return SaveDeliveryAddressResultDto.Failed("Recipient name is required.");
        }

        if (command.RecipientName.Length > 200)
        {
            return SaveDeliveryAddressResultDto.Failed("Recipient name cannot exceed 200 characters.");
        }

        // Validate country is in allowed regions
        if (!IsCountryAllowed(command.Country))
        {
            return SaveDeliveryAddressResultDto.Failed(
                $"We currently do not ship to {command.Country}. Please select a different address.");
        }

        DeliveryAddress address;

        if (command.AddressId.HasValue)
        {
            // Update existing address
            address = await _addressRepository.GetByIdAsync(command.AddressId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Address not found.");

            // Verify ownership
            if (command.BuyerId.HasValue && address.BuyerId != command.BuyerId)
            {
                return SaveDeliveryAddressResultDto.Failed("Address not found.");
            }

            address.Update(command.RecipientName, addressValue, command.PhoneNumber, command.Label);
        }
        else
        {
            // Create new address
            if (command.BuyerId.HasValue && command.BuyerId.Value != Guid.Empty)
            {
                address = new DeliveryAddress(
                    command.BuyerId.Value,
                    command.RecipientName,
                    addressValue,
                    command.PhoneNumber,
                    command.Label,
                    command.SetAsDefault);
            }
            else if (!string.IsNullOrWhiteSpace(command.SessionId))
            {
                address = new DeliveryAddress(
                    command.SessionId,
                    command.RecipientName,
                    addressValue,
                    command.PhoneNumber);
            }
            else
            {
                return SaveDeliveryAddressResultDto.Failed("Either buyer ID or session ID is required.");
            }

            await _addressRepository.AddAsync(address, cancellationToken);
        }

        // Handle default address logic
        if (command.SetAsDefault && command.BuyerId.HasValue)
        {
            // Clear default from other addresses
            var existingAddresses = await _addressRepository.GetByBuyerIdAsync(command.BuyerId.Value, cancellationToken);
            foreach (var existingAddress in existingAddresses)
            {
                if (existingAddress.Id != address.Id && existingAddress.IsDefault)
                {
                    existingAddress.RemoveDefault();
                    await _addressRepository.UpdateAsync(existingAddress, cancellationToken);
                }
            }
            address.SetAsDefault();
        }

        // Only call UpdateAsync for existing addresses (when AddressId was provided)
        if (command.AddressId.HasValue)
        {
            await _addressRepository.UpdateAsync(address, cancellationToken);
        }

        await _addressRepository.SaveChangesAsync(cancellationToken);

        return SaveDeliveryAddressResultDto.Succeeded(MapToDto(address));
    }

    /// <summary>
    /// Sets an address as the default for a buyer.
    /// </summary>
    public async Task<SetDefaultAddressResultDto> HandleAsync(
        SetDefaultAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var address = await _addressRepository.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null || address.BuyerId != command.BuyerId)
        {
            return SetDefaultAddressResultDto.Failed("Address not found.");
        }

        // Clear default from other addresses
        var existingAddresses = await _addressRepository.GetByBuyerIdAsync(command.BuyerId, cancellationToken);
        foreach (var existingAddress in existingAddresses)
        {
            if (existingAddress.Id != command.AddressId && existingAddress.IsDefault)
            {
                existingAddress.RemoveDefault();
                await _addressRepository.UpdateAsync(existingAddress, cancellationToken);
            }
        }

        address.SetAsDefault();
        await _addressRepository.UpdateAsync(address, cancellationToken);
        await _addressRepository.SaveChangesAsync(cancellationToken);

        return SetDefaultAddressResultDto.Succeeded();
    }

    /// <summary>
    /// Deletes (deactivates) a delivery address.
    /// </summary>
    public async Task<DeleteDeliveryAddressResultDto> HandleAsync(
        DeleteDeliveryAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var address = await _addressRepository.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null || address.BuyerId != command.BuyerId)
        {
            return DeleteDeliveryAddressResultDto.Failed("Address not found.");
        }

        address.Deactivate();
        await _addressRepository.UpdateAsync(address, cancellationToken);
        await _addressRepository.SaveChangesAsync(cancellationToken);

        return DeleteDeliveryAddressResultDto.Succeeded();
    }

    /// <summary>
    /// Validates if shipping is available to the specified address for cart items.
    /// </summary>
    public async Task<ValidateShippingResultDto> HandleAsync(
        ValidateShippingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if country is in allowed regions
        if (!IsCountryAllowed(command.Country))
        {
            return ValidateShippingResultDto.RegionRestricted(
                $"We currently do not ship to {command.Country}.",
                Array.Empty<string>());
        }

        // Get cart and check for any product-specific restrictions
        Cart? cart = null;
        if (command.BuyerId.HasValue && command.BuyerId.Value != Guid.Empty)
        {
            cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(command.SessionId))
        {
            cart = await _cartRepository.GetBySessionIdAsync(command.SessionId, cancellationToken);
        }

        if (cart is null || cart.Items.Count == 0)
        {
            return ValidateShippingResultDto.Success();
        }

        // Get products to check for any restrictions
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        // For now, all products can ship to allowed countries
        // In future, products could have region restrictions
        var restrictedProducts = new List<string>();

        if (restrictedProducts.Count > 0)
        {
            return ValidateShippingResultDto.RegionRestricted(
                "Some items in your cart cannot be shipped to your region.",
                restrictedProducts.AsReadOnly());
        }

        return ValidateShippingResultDto.Success();
    }

    private static bool IsCountryAllowed(string country)
    {
        return AllowedCountries.Contains(country.Trim());
    }

    private static DeliveryAddressDto MapToDto(DeliveryAddress address)
    {
        return new DeliveryAddressDto(
            address.Id,
            address.RecipientName,
            address.PhoneNumber,
            address.Label,
            address.Street,
            address.Street2,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.IsDefault,
            address.CreatedAt,
            address.UpdatedAt);
    }
}
