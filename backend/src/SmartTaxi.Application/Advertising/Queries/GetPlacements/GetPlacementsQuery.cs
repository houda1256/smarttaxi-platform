using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetPlacements;

public sealed record GetPlacementsQuery : IQuery<IReadOnlyCollection<AdvertisingPlacement>>;
