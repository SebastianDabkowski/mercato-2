namespace SD.Project.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary values with a currency code.
/// </summary>
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public override string ToString() => $"{Currency} {Amount:F2}";
}
