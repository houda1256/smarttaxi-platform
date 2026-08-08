using System.Diagnostics.CodeAnalysis;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Payments.ValueObjects;

/// <summary>
/// Kept module-local (mirrors Fleet.Expenses' own Money) rather than shared
/// from Fleet, to avoid Payments.Domain depending on a Fleet-specific
/// namespace for a basic value object — modules stay isolated per the
/// project's own module-separation rule.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (!TryCreate(amount, currency, out var money, out var error))
        {
            throw new ArgumentException(error);
        }

        return money;
    }

    public static bool TryCreate(decimal amount, string? currency, [NotNullWhen(true)] out Money? money, [NotNullWhen(false)] out string? error)
    {
        if (amount < 0)
        {
            money = null;
            error = "Le montant ne peut pas être négatif.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            money = null;
            error = "La devise doit être un code ISO à 3 lettres.";
            return false;
        }

        money = new Money(amount, currency.Trim().ToUpperInvariant());
        error = null;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
