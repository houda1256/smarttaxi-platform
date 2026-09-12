using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>Admin-managed catalog of where a campaign's media can run — a generic Code/Name pair, deliberately not a React screen-name enum, so new placements never require a code change.</summary>
public sealed class AdvertisingPlacement : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IReadOnlyCollection<AdMediaType> SupportedMediaTypes { get; private set; } = [];
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private AdvertisingPlacement()
    {
    }

    private AdvertisingPlacement(
        string code, string name, string description, IReadOnlyCollection<AdMediaType> supportedMediaTypes, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        Description = description;
        SupportedMediaTypes = supportedMediaTypes;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static AdvertisingPlacement Create(
        string code, string name, string description, IReadOnlyCollection<AdMediaType> supportedMediaTypes, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de l'emplacement est requis.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom de l'emplacement est requis.");
        }

        if (supportedMediaTypes.Count == 0)
        {
            throw new ArgumentException("Au moins un type de média doit être supporté.");
        }

        return new AdvertisingPlacement(code.Trim().ToUpperInvariant(), name.Trim(), (description ?? string.Empty).Trim(), supportedMediaTypes, utcNow);
    }

    public void Update(string name, string description, IReadOnlyCollection<AdMediaType> supportedMediaTypes, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom de l'emplacement est requis.");
        }

        if (supportedMediaTypes.Count == 0)
        {
            throw new ArgumentException("Au moins un type de média doit être supporté.");
        }

        Name = name.Trim();
        Description = (description ?? string.Empty).Trim();
        SupportedMediaTypes = supportedMediaTypes;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public bool Supports(AdMediaType mediaType) => SupportedMediaTypes.Contains(mediaType);
}
