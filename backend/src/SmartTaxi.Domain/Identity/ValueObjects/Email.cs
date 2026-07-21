using System.Text.RegularExpressions;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Identity.ValueObjects;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("L'email ne peut pas être vide.", nameof(value));
        }

        var normalized = value.Trim();

        if (!EmailRegex().IsMatch(normalized))
        {
            throw new ArgumentException("Le format de l'email est invalide.", nameof(value));
        }

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
