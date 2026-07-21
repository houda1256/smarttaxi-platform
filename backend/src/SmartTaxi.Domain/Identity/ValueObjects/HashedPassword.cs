using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Identity.ValueObjects;

public sealed class HashedPassword : ValueObject
{
    public string Value { get; }

    private HashedPassword(string value)
    {
        Value = value;
    }

    public static HashedPassword Create(string hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
        {
            throw new ArgumentException("Le mot de passe haché ne peut pas être vide.", nameof(hashedValue));
        }

        return new HashedPassword(hashedValue);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
