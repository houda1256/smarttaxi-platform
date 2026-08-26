using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.API.Contracts.Advertising;

public sealed record CreatePlacementRequest(string Code, string Name, string Description, IReadOnlyList<string> SupportedMediaTypes);

public sealed record UpdatePlacementRequest(string Name, string Description, IReadOnlyList<string> SupportedMediaTypes);

public sealed record AdvertisingPlacementResponse(Guid Id, string Code, string Name, string Description, IReadOnlyCollection<string> SupportedMediaTypes, bool IsActive)
{
    public static AdvertisingPlacementResponse FromEntity(AdvertisingPlacement placement) =>
        new(placement.Id, placement.Code, placement.Name, placement.Description, placement.SupportedMediaTypes.Select(t => t.ToString()).ToList(), placement.IsActive);
}
