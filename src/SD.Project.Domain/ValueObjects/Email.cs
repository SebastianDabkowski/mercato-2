using System.Text.RegularExpressions;

namespace SD.Project.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated email address.
/// </summary>
public sealed partial record Email
{
    private static readonly Regex EmailRegex = CreateEmailRegex();

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an Email value object if the provided string is a valid email address.
    /// </summary>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email address is required.", nameof(email));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalizedEmail))
        {
            throw new ArgumentException("Email address format is invalid.", nameof(email));
        }

        if (normalizedEmail.Length > 254)
        {
            throw new ArgumentException("Email address is too long.", nameof(email));
        }

        return new Email(normalizedEmail);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex CreateEmailRegex();
}
