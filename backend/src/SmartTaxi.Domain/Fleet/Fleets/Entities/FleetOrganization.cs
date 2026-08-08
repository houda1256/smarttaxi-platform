using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Fleets.Enums;
using SmartTaxi.Domain.Fleet.Fleets.Events;

namespace SmartTaxi.Domain.Fleet.Fleets.Entities;

/// <summary>
/// The "Fleets" table's entity is named FleetOrganization (not Fleet) purely
/// to avoid any ambiguity with the SmartTaxi.Domain.Fleet module namespace
/// itself — same entity the master prompt calls "Fleet" conceptually.
/// </summary>
public sealed class FleetOrganization : AggregateRoot
{
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid CityId { get; private set; }
    public FleetStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private FleetOrganization()
    {
    }

    private FleetOrganization(Guid ownerId, string name, string? description, Guid cityId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        OwnerId = ownerId;
        Name = name;
        Description = description;
        CityId = cityId;
        Status = FleetStatus.Active;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new FleetCreated(Id, ownerId, utcNow));
    }

    public static FleetOrganization Create(Guid ownerId, string name, string? description, Guid cityId, DateTime utcNow) =>
        new(ownerId, name, description, cityId, utcNow);

    public void UpdateDetails(string name, string? description, Guid cityId, DateTime utcNow)
    {
        Name = name;
        Description = description;
        CityId = cityId;
        UpdatedAt = utcNow;
    }
}
