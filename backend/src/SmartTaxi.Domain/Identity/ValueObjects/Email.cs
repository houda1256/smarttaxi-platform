using System.Diagnostics.CodeAnalysis;
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
        if (!TryCreate(value, out var email, out var error))
        {
            throw new ArgumentException(error, nameof(value));
        }

        return email;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Email? email, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            email = null;
            error = "L'email ne peut pas être vide.";
            return false;
        }

        var normalized = value.Trim();

        if (!EmailRegex().IsMatch(normalized))
        {
            email = null;
            error = "Le format de l'email est invalide.";
            return false;
        }

        email = new Email(normalized);
        error = null;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
