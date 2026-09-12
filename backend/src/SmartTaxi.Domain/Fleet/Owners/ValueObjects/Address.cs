using System.Diagnostics.CodeAnalysis;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Owners.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string? PostalCode { get; }
    public string Country { get; }

    private Address(string street, string city, string? postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static Address Create(string street, string city, string? postalCode, string country)
    {
        if (!TryCreate(street, city, postalCode, country, out var address, out var error))
        {
            throw new ArgumentException(error);
        }

        return address;
    }

    public static bool TryCreate(
        string? street, string? city, string? postalCode, string? country,
        [NotNullWhen(true)] out Address? address, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
        {
            address = null;
            error = "La rue, la ville et le pays sont requis.";
            return false;
        }

        address = new Address(street.Trim(), city.Trim(), postalCode?.Trim(), country.Trim());
        error = null;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }
}
