using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetPlacementsAdmin;

public sealed record GetPlacementsAdminQuery : IQuery<IReadOnlyCollection<AdvertisingPlacement>>;
