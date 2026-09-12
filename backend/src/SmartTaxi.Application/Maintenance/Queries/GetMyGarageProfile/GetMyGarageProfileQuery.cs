using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyGarageProfile;

public sealed record GetMyGarageProfileQuery(Guid UserId) : IQuery<GarageProfile?>;
