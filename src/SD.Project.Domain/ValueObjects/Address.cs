namespace SD.Project.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated delivery address.
/// Supports multiple countries where Mercato operates.
/// </summary>
public sealed record Address
{
    public string Street { get; }
    public string? Street2 { get; }
    public string City { get; }
    public string? State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string street, string? street2, string city, string? state, string postalCode, string country)
    {
        Street = street;
        Street2 = street2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>
    /// Creates an Address value object with validation.
    /// </summary>
    public static Address Create(
        string street,
        string? street2,
        string city,
        string? state,
        string postalCode,
        string country)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new ArgumentException("Street address is required.", nameof(street));
        }

        if (street.Length > 200)
        {
            throw new ArgumentException("Street address cannot exceed 200 characters.", nameof(street));
        }

        if (street2 is not null && street2.Length > 200)
        {
            throw new ArgumentException("Street address line 2 cannot exceed 200 characters.", nameof(street2));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City is required.", nameof(city));
        }

        if (city.Length > 100)
        {
            throw new ArgumentException("City cannot exceed 100 characters.", nameof(city));
        }

        if (state is not null && state.Length > 100)
        {
            throw new ArgumentException("State/Province cannot exceed 100 characters.", nameof(state));
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new ArgumentException("Postal code is required.", nameof(postalCode));
        }

        if (postalCode.Length > 20)
        {
            throw new ArgumentException("Postal code cannot exceed 20 characters.", nameof(postalCode));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.", nameof(country));
        }

        if (country.Length > 100)
        {
            throw new ArgumentException("Country cannot exceed 100 characters.", nameof(country));
        }

        return new Address(
            street.Trim(),
            street2?.Trim(),
            city.Trim(),
            state?.Trim(),
            postalCode.Trim(),
            country.Trim());
    }

    /// <summary>
    /// Returns a formatted single-line address string.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string> { Street };
        if (!string.IsNullOrWhiteSpace(Street2))
        {
            parts.Add(Street2);
        }
        parts.Add(City);
        if (!string.IsNullOrWhiteSpace(State))
        {
            parts.Add(State);
        }
        parts.Add(PostalCode);
        parts.Add(Country);

        return string.Join(", ", parts);
    }
}
