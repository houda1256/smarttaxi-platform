using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Identity.ValueObjects;

public sealed partial class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string value)
    {
        if (!TryCreate(value, out var phoneNumber, out var error))
        {
            throw new ArgumentException(error, nameof(value));
        }

        return phoneNumber;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out PhoneNumber? phoneNumber, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            phoneNumber = null;
            error = "Le numéro de téléphone ne peut pas être vide.";
            return false;
        }

        var normalized = value.Trim();

        if (!PhoneNumberRegex().IsMatch(normalized))
        {
            phoneNumber = null;
            error = "Le format du numéro de téléphone est invalide (format E.164 attendu, ex. +21612345678).";
            return false;
        }

        phoneNumber = new PhoneNumber(normalized);
        error = null;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    // E.164: '+' followed by 8 to 15 digits, first digit 1-9.
    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex PhoneNumberRegex();
}
